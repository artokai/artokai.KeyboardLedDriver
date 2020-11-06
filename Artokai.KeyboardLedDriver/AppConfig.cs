using Microsoft.Extensions.Configuration;

namespace Artokai.KeyboardLedDriver
{
    public static class AppConfig
    {
        public static IConfiguration Configuration { get; set; }

        public static T GetSection<T>() where T : new()
        {
            T result = default(T);
            if (typeof(T) == typeof(ErrorPollingConfig))
                result = Configuration.GetSection("errorPolling").Get<T>();

            if (result == null)
                result = new T();

            return result;
        }
    }
    public class ErrorPollingConfig
    {
        public bool Enabled { get; set; } = false;
        public int Interval { get; set; } = 15;
        public string Url { get; set; } = "";
    }
}
