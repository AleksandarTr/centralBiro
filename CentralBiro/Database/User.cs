using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

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

    public User() : this("", new byte[0], new byte[0], -1) { }

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