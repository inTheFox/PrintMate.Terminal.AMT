namespace Observer.Shared.Models;

public class Services
{
    private static string ServicesDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services");

    public static ServiceInfo Hans1 = new ServiceInfo
    {
        Id = "hans_227",
        Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HansHostProvider.exe"),
        Url = "http://localhost:21758",
        StartupArguments = "172.18.34.227"
    };

    public static ServiceInfo Hans2 = new ServiceInfo
    {
        Id = "hans_228",
        Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HansHostProvider.exe"),
        Url = "http://localhost:21759",
        StartupArguments = "172.18.34.228"
    };

    public static ServiceInfo LoggingService = new ServiceInfo
    {
        Id = "logging_service",
        Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LoggingService.exe"),
        Url = "http://localhost:5201",
        StartupArguments = ""
    };
}