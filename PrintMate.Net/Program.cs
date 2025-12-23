
using Microsoft.EntityFrameworkCore;
using PrintMate.Net.Database;

namespace PrintMate.Net
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(new string[]{"--urls", "http://localhost:21758"});

            // Add services to the container.

            builder.Services.AddDbContext<DatabaseContext>(p =>
                p.UseMySql("server=localhost;user=root;password=102030;database=printmate",
                    ServerVersion.Parse("11.4.9-MariaDB")));

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
