using Hans.NET.Models;

namespace HansHostProvider.Services
{
    /// <summary>
    /// Конфигурация сервиса Hans Host Provider
    /// </summary>
    public sealed class ServiceOptions
    {
        public string BoardAddress { get; set; } = string.Empty;
        public string ServiceUrl { get; set; } = string.Empty;
        public ScanatorConfiguration? Configuration { get; set; }
    }

    /// <summary>
    /// Статический контекст для быстрого доступа к конфигурации (legacy)
    /// </summary>
    public static class ServiceContext
    {
        public static string BoardAddress { get; set; } = string.Empty;
        public static string ServiceUrl { get; set; } = string.Empty;
        public static ScanatorConfiguration? Configuration { get; set; }

        public static void Initialize(ServiceOptions options)
        {
            BoardAddress = options.BoardAddress;
            ServiceUrl = options.ServiceUrl;
            Configuration = options.Configuration;
        }
    }
}
