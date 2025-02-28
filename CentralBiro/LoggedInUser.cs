using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro;

[PrimaryKey(nameof(Token))]
public class LoggedInUser(User user, byte[] token)
{
    public const int LoginDuration = 120;
    
    public User User { get; set; } = user;
    [StringLength(20)]
    public byte[] Token { get; set; } = token;
    public DateTime Expiration { get; set; } = DateTime.Now.AddMinutes(LoginDuration);

    public LoggedInUser() : this(null, new byte[0]) {}
}