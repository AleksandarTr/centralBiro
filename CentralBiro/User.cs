using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro;

[Index(nameof(Username), IsUnique = true)]
[PrimaryKey(nameof(Id))]
public class User(string username, byte[] password, byte[] salt, int id)
{
    [StringLength(32)]
    public string Username { get; set; } = username;
    public byte[] Password { get; set; } = password;
    public byte[] Salt { get; set; } = salt;
    public int Id { get; set; } = id;

    public User() : this("", new byte[0], new byte[0], -1) { }

    public User(string username, byte[] password, byte[] salt) : this("", new byte[0], new byte[0], 0)
    {
        User? user = new CentralContext().Users.FromSql($"Select max(id) from Users").FirstOrDefault();
        if (user != null) Id = user.Id + 1;
        else Id = 0;
    }
}