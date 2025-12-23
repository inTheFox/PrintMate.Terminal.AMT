using LoggingService.Data;
using LoggingService.Hubs;
using LoggingService.Services;
using System.Diagnostics;

if (args.Length == 0)
{
    Console.WriteLine("Usage: LoggingService <url>");
    Console.WriteLine("  url: URL to host the service (e.g., http://localhost:5100)");
    return;
}

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

var builder = WebApplication.CreateBuilder(new[] { "--urls", args[0] });

// Add services to the container.
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<FileLogStorage>();
builder.Services.AddHostedService(p => p.GetRequiredService<FileLogStorage>());
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseStaticFiles();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.MapHub<LogsHub>("/logsHub");

app.Run();
