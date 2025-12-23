using Microsoft.EntityFrameworkCore;

namespace PrintMate.Net.Database;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
        
    }
}