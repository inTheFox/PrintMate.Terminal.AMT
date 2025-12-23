using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Parsers.Shared.Models;
using PrintSpectator.Shared.Models;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Database
{
    public class DatabaseContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<ProjectInfo> Projects { get; set; }
        public DbSet<PrintSession> PrintSessions { get; set; }
        public DbSet<LayerState> LayersStates { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public DatabaseContext() : base()
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Путь к папке с исполняемым файлом
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                // Полный путь к папке database
                string dbDirectory = Path.Combine(baseDirectory, "database");
                // Убедимся, что папка существует
                Directory.CreateDirectory(dbDirectory);
                // Путь к файлу базы данных
                string dbPath = Path.Combine(dbDirectory, "printmate.db");

                optionsBuilder.UseSqlite($"Data source={dbPath}");
            }
        }
    }

    /// <summary>
    /// Design-time factory для EF Core миграций.
    /// Позволяет запускать Add-Migration из PMC без запуска WPF-приложения.
    /// </summary>
    public class DatabaseContextDesignTimeFactory : IDesignTimeDbContextFactory<DatabaseContext>
    {
        public DatabaseContext CreateDbContext(string[] args)
        {
            return new DatabaseContext();
        }
    }
}
