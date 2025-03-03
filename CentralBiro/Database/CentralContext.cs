using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

public class CentralContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<LoggedInUser> LoggedInUsers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=cb.db");
    }

    public CentralContext()
    {
        Database.EnsureCreated();
    }
}