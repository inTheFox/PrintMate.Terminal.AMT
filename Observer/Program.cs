using Observer.Services;

namespace Observer
{
    public class Program
    {
        private static WebApplication _app;
        public async static Task Main(string[] args)
        {
            Task.Factory.StartNew(async () =>
            {
                Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);

                var builder = WebApplication.CreateBuilder(args);
                builder.WebHost.UseUrls("http://localhost:21720");

                // Add services to the container.
                builder.Services.AddSingleton<ServiceWorker>();
                builder.Services.AddHostedService(p => p.GetRequiredService<ServiceWorker>());
                builder.Services.AddScoped<ServiceMonitorService>();
                builder.Services.AddControllers();

                // Add Blazor Server
                builder.Services.AddRazorPages();
                builder.Services.AddServerSideBlazor();

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                }

                app.UseStaticFiles();
                app.UseRouting();

                app.MapControllers();
                app.MapBlazorHub();
                app.MapRazorPages();
                app.MapFallbackToPage("/_Host");

                _app = app;
                app.Run();
            });

            while (true)
            {
                if (Console.ReadLine() == "exit")
                {
                    await _app.StopAsync();
                    Environment.Exit(0);
                }
            }
        }
    }
}
