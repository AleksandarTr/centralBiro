using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

/// <summary>
/// <c>CentralContext</c> is a session with an SQLite database made for this project.
/// </summary>
public class CentralContext : DbContext
{
    ///<value>
    /// Access point for the <c>user</c> table, containing user information <br/>
    /// Columns:
    /// <list type="table">
    /// <listheader>
    /// <term>Name</term>
    /// <description>Type</description>
    /// </listheader>
    /// <item>
    /// <term>Id</term>
    /// <description>int</description>
    /// </item>
    /// <item>
    /// <term>Username</term>
    /// <description>string(32)</description>
    /// </item>
    /// <item>
    /// <term>Password</term>
    /// <description>byte[]</description>
    /// </item>
    /// <item>
    /// <term>Salt</term>
    /// <description>byte[]</description>
    /// </item>
    /// </list>
    /// </value>
    public DbSet<User> Users { get; set; }
    ///<value>
    /// Access point for the <c>logged_in_user</c> table, containing active user sessions <br/>
    /// Columns:
    /// <list type="table">
    /// <listheader>
    /// <term>Name</term>
    /// <description>Type</description>
    /// </listheader>
    /// <item>
    /// <term>User</term>
    /// <description><see cref="User"/></description>
    /// </item>
    /// <item>
    /// <term>Token</term>
    /// <description>byte[]</description>
    /// </item>
    /// <item>
    /// <term>Expiration</term>
    /// <description>Datetime</description>
    /// </item>
    /// </list>
    /// </value>
    public DbSet<LoggedInUser> LoggedInUsers { get; set; }
    public DbSet<ProductType> ProductTypes { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        SqliteConnection conn = new SqliteConnection("Data Source=CentralDatabase.db");
        conn.Open();

        SqliteCommand command = conn.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;PRAGMA busy_timeout=5000;";
        command.ExecuteNonQuery();
        
        optionsBuilder.UseSqlite(conn);
    }

    static CentralContext()
    {
        new CentralContext().Database.EnsureCreated();
    }
}