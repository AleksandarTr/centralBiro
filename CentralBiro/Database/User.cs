using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

/// <summary>
/// <c>User</c> class is used to transfer data about a user from the database
/// </summary>
/// <param name="username">User's username</param>
/// <param name="password">User's salt-hashed password</param>
/// <param name="salt">The salt used for hashing the user's password</param>
/// <param name="id">User's unique id</param>
[Index(nameof(Username), IsUnique = true)]
[PrimaryKey(nameof(Id))]
[Table("User")]
public class User(string username, byte[] password, byte[] salt, int id)
{
    [StringLength(32)]
    public string Username { get; set; } = username;
    public byte[] Password { get; set; } = password;
    public byte[] Salt { get; set; } = salt;
    public int Id { get; set; } = id;

    /// <summary>
    /// <c>User</c> class is used to transfer data about a user from the database. This constructor creates a
    /// blank user class, which is not intended to be stored in the database.
    /// </summary>
    public User() : this("", new byte[0], new byte[0], -1) { }

    /// <summary>
    /// <c>User</c> class is used to transfer data about a user from the database. This constructor auto assigns
    /// the next id for the user
    /// </summary>
    /// <param name="username">User's username</param>
    /// <param name="password">User's salt-hashed password</param>
    /// <param name="salt">The salt used for hashing the user's password</param>
    public User(string username, byte[] password, byte[] salt) : this(username, password, salt, 0)
    {
        try
        {
            Id = new CentralContext().Users.Max(user => user.Id) + 1;
        }
        catch (InvalidOperationException)
        {
            Id = 0;
        }
    }
}