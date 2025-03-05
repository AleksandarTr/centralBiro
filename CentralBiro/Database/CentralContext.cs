using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

public class CentralContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<LoggedInUser> LoggedInUsers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductType> ProductTypes { get; set; }
    public DbSet<ProductMetadata> ProductMetadata { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=cb.db");
    }

    public CentralContext()
    {
        Database.EnsureCreated();
    }
}