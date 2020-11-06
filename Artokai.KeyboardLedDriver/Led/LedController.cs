using LedCSharp;
using System;

namespace Artokai.KeyboardLedDriver.Led
{
    public class LedController : IDisposable
    {
        private readonly keyboardNames[] ALERT_KEYS = {
            keyboardNames.ESC,
            keyboardNames.F1,
            keyboardNames.F2,
            keyboardNames.F3,
            keyboardNames.F4,
            keyboardNames.F5,
            keyboardNames.F6,
            keyboardNames.F7,
            keyboardNames.F8,
            keyboardNames.F9,
            keyboardNames.F10,
            keyboardNames.F11,
            keyboardNames.F12,
            keyboardNames.PRINT_SCREEN,
            keyboardNames.SCROLL_LOCK,
            keyboardNames.PAUSE_BREAK
        };

        private bool disposed = false;

        public bool IsInitialized { get; private set; }
        public ColorScheme CurrentColorScheme { get; private set; }
        public bool CurrentAlertState { get; private set; } = false;

        public bool Initialize()
        {
            IsInitialized = LogitechGSDK.LogiLedInit();
            return IsInitialized;
        }

        public void ShutDown()
        {
            if (IsInitialized)
            {
                LogitechGSDK.LogiLedShutdown();
                IsInitialized = false;
            }
        }

        public void ToggleAlert(bool alertState)
        {
            if (!IsInitialized) { return; }
            SetColorScheme(this.CurrentColorScheme, alertState);
        }

        public void SetColorScheme(ColorScheme scheme, bool showAlert)
        {
            if (!IsInitialized) { return; }
            LogitechGSDK.LogiLedStopEffects();

            // Set default color scheme
            CurrentColorScheme = scheme;
            LogitechGSDK.LogiLedSetLighting(100 * scheme.R / 255, 100 * scheme.G / 255, 100 * scheme.B / 255);

            // Colorize the logo
            LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(keyboardNames.G_LOGO, 100 * 50 / 255, 100 * 100 / 255, 100 * 50 / 255);

            // Show alerts
            CurrentAlertState = showAlert;
            if (showAlert)
            {
                foreach (var key in ALERT_KEYS)
                {
                    LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(key, 100 * 177 / 255, 100 * 16 / 255, 100 * 46 / 255);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any managed objects here.
            }

            // Free any unmanaged objects here.
            ShutDown();
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~LedController()
        {
            Dispose(false);
        }
    }
}
