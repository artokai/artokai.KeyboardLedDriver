using Artokai.KeyboardLedDriver.Led;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Artokai.KeyboardLedDriver
{
    public class Worker
    {
        private CancellationTokenSource cts;
        private DateTimeOffset LastNetworkCheck = DateTimeOffset.MinValue;
        private ErrorPollingConfig ErrorPollingConfig = AppConfig.GetSection<ErrorPollingConfig>();
        private DateTimeOffset LastErrorCheck = DateTimeOffset.MinValue;
        private readonly HttpClient ErrorCheckClient = new HttpClient();
        public bool IsRunning { get; private set; }
        private ColorScheme DesiredScheme = ColorScheme.Default;
        private bool ShowAlert = false;

        public void Run()
        {
            if (IsRunning) { throw new ApplicationException("Already running!"); }
            IsRunning = true;
            cts = new CancellationTokenSource();

            bool isLGHUBAgentRunning = WaitForLGHUBAgent();
            if (isLGHUBAgentRunning) {
                using var controller = new LedController();
                while (!cts.IsCancellationRequested)
                {
                    if (!controller.IsInitialized)
                        controller.Initialize();

                    if (controller.IsInitialized)
                        OnTick(controller);

                    Task.Delay(TimeSpan.FromSeconds(1), cts.Token).Wait();
                }
                controller.ShutDown();
            }
            IsRunning = false;
        }

        private bool WaitForLGHUBAgent()
        {
            var lghubAgentRunning = false;
            var waitStart = DateTimeOffset.Now;
            bool shouldWait = true;
            while (shouldWait)
            {
                var lghubAgentWasJustStarted = false;
                var p = Process.GetProcessesByName("lghub_agent");
                if (p != null && p.Length > 0)
                {
                    lghubAgentRunning = true;
                    var agentRunningTime = DateTime.Now - p[0].StartTime;
                    lghubAgentWasJustStarted = agentRunningTime.TotalSeconds < 30;
                }

                if (!lghubAgentRunning || lghubAgentWasJustStarted)
                {
                    Task.Delay(TimeSpan.FromSeconds(30), cts.Token).Wait();
                }
                var totalWaitTime = (DateTimeOffset.Now - waitStart).TotalSeconds;
                shouldWait = !cts.IsCancellationRequested && !lghubAgentRunning && (totalWaitTime < 300);
            }
            return lghubAgentRunning;
        }

        private void OnTick(LedController controller)
        {
            // Toggle the leds if necessary
            if (DesiredScheme != controller.CurrentColorScheme || ShowAlert != controller.CurrentAlertState)
            {
                controller.SetColorScheme(DesiredScheme, ShowAlert);
            }

            // Check VPN status every 30 seconds (also triggered when network changes)
            if ((DateTimeOffset.Now - LastNetworkCheck).TotalSeconds > 30)
            {
                var vpnState = NetworkHelper.IsVpnOn();
                LastNetworkCheck = DateTimeOffset.Now;
                DesiredScheme = vpnState ? ColorScheme.Vpn : ColorScheme.Default;
            }

            // Check for alerts
            if (ErrorPollingConfig.Enabled && !string.IsNullOrEmpty(ErrorPollingConfig.Url))
            {
                if ((DateTimeOffset.Now - LastErrorCheck).TotalSeconds > ErrorPollingConfig.Interval * 60)
                {
                    CheckForErrors();
                }
            }
        }

        private void CheckForErrors()
        {
            try
            {
                LastErrorCheck = DateTimeOffset.Now;

                var reqTask = ErrorCheckClient.GetAsync(ErrorPollingConfig.Url);
                reqTask.Wait(cts.Token);
                if (!reqTask.IsCompletedSuccessfully)
                    return;

                var response = reqTask.Result;
                response.EnsureSuccessStatusCode();
                var rdrTask = response.Content.ReadAsStringAsync();
                rdrTask.Wait(cts.Token);
                if (!rdrTask.IsCompletedSuccessfully)
                    return;

                var content = rdrTask.Result;
                var responseObj = JsonSerializer.Deserialize<ErrorPollingResponse>(content);
                ShowAlert = (responseObj.Status?.ToLower() != "ok");
            } catch {
                // no-op
            }
        }

        internal void QueueNetworkCheck()
        {
            LastNetworkCheck = DateTimeOffset.MinValue;
        }

        public void Stop()
        {
            cts?.Cancel();
        }
    }
}
