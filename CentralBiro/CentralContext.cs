using Microsoft.EntityFrameworkCore;

namespace CentralBiro;

public class CentralContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<LoggedInUser> LoggedInUsers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=centralbiro.db");
    }

    public CentralContext()
    {
        Database.EnsureCreated();
    }
}