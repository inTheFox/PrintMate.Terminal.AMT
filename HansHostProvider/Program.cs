using System.Diagnostics;
using HansHostProvider.Hubs;
using HansHostProvider.Services;
using HansHostProvider.Utils;

namespace HansHostProvider
{
    public class Program
    {
        public static void Main(string[] args)
        {


            if (!ValidateArguments(args))
                return;

            // Ёлегантное завершение процесса, если процесс "Observer" не запущен (ебучий костыль конечно)
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var observerExists = Process.GetProcessesByName("Observer").Any();
                    if (!observerExists)
                    {
                        Process.GetCurrentProcess().Kill();
                    }
                    await Task.Delay(3000);
                }
            });

            var serviceUrl = args[0];
            var boardAddress = args[1];

            InitializeServiceContext(serviceUrl, boardAddress);
            PrintStartupInfo(serviceUrl, boardAddress);
            InstallHooks();

            var app = BuildApplication(serviceUrl);
            app.Run();
        }

        private static bool ValidateArguments(string[] args)
        {
            if (args.Length >= 2)
                return true;

            Console.WriteLine("Usage: HansHostProvider <service-url> <board-ip-address>");
            Console.WriteLine("  service-url:       URL to host the service (e.g., http://localhost:5000)");
            Console.WriteLine("  board-ip-address:  IP address of the scanner board (e.g., 172.18.34.227)");
            return false;
        }

        private static void InitializeServiceContext(string serviceUrl, string boardAddress)
        {
            ServiceContext.Initialize(new ServiceOptions
            {
                ServiceUrl = serviceUrl,
                BoardAddress = boardAddress
            });
        }

        private static void PrintStartupInfo(string serviceUrl, string boardAddress)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("HansHostProvider");
            Console.WriteLine($"  URL:      {serviceUrl}");
            Console.WriteLine($"  Board IP: {boardAddress}");
            Console.WriteLine($"  PID:      {Environment.ProcessId}");
            Console.WriteLine("===========================================");
        }

        private static void InstallHooks()
        {
            Console.WriteLine("[Init] Installing API hooks for mutex bypass...");
            ApiHook.InstallHook();
            MutexHook.Initialize();
            Console.WriteLine("[Init] Hooks installed - SDK mutexes will be process-specific");
        }

        private static WebApplication BuildApplication(string serviceUrl)
        {
            var builder = WebApplication.CreateBuilder(new[] { "--urls", serviceUrl });

            // Services
            builder.Services.AddControllers();
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<HansService>();
            builder.Services.AddSingleton<IHostedService>(p => p.GetRequiredService<HansService>());

            var app = builder.Build();

            // Endpoints
            app.MapHub<EventsHub>("/events");
            app.MapHub<InvokeHub>("/invoke");
            app.MapControllers();

            // Middleware
            app.UseHttpsRedirection();
            app.UseAuthorization();

            return app;
        }
    }
}
