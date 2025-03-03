using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

[PrimaryKey(nameof(Token))]
[Table("LoggedInUser")]
public class LoggedInUser(User user, byte[] token)
{
    public const int LoginDuration = 120;
    
    public User User { get; set; } = user;
    [StringLength(20)]
    public byte[] Token { get; set; } = token;
    public DateTime Expiration { get; set; } = DateTime.Now.AddMinutes(LoginDuration);

    public LoggedInUser() : this(null, new byte[0]) {}
}