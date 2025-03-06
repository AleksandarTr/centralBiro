using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductType> ProductTypes { get; set; }
    public DbSet<ProductMetadata> ProductMetadata { get; set; }

    /// <summary>
    /// <c>WalInterceptor</c> is a wrapper class used to enable WAL journal mode in the database, so that
    /// it has better write concurrency.
    /// </summary>
    private class WalInterceptor : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            if (connection is SqliteConnection)
            {
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA journal_mode=WAL";
                command.ExecuteNonQuery();
            }
            return new ValueTask<InterceptionResult>(result);
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=cb.db;BusyTimeout=5000");
        optionsBuilder.AddInterceptors(new WalInterceptor());
    }

    public CentralContext()
    {
        Database.EnsureCreated();
    }
}