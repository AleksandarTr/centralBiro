using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

/// <summary>
/// <c>LoggedInUser</c> class represents an instance of a user's log-in session and is used to check for
/// its validity and expiration
/// </summary>
/// <param name="user">Instance of the <c>User</c> class for the user who has logged in</param>
/// <param name="token">Number used to track and verify the user's identity</param>
[PrimaryKey(nameof(Token))]
[Table("logged_in_user")]
public class LoggedInUser(User user, byte[] token)
{
    /// <value>
    /// Number of minutes after the user's login after which a cleanup thread will be able to remove the session.<br/>
    /// Every subsequent user request also extends the session to this number of minutes post the current moment.
    /// </value>
    public const int LoginDuration = 120;
    
    ///<value>
    /// Instance of the <c>User</c> class for the user who has logged in
    /// </value>
    public User User { get; set; } = user;
    /// <value>
    /// Number used to track and verify the user's identity
    /// </value>
    [StringLength(20)]
    public byte[] Token { get; set; } = token;
    /// <value>
    /// <c>DateTime</c> instance representing the moment after which the cleanup thread can remove this session.
    /// </value>
    public DateTime Expiration { get; set; } = DateTime.Now.AddMinutes(LoginDuration);

    /// <summary>
    /// <c>LoggedInUser</c> class represents an instance of a user's log-in session and is used to check for
    /// its validity and expiration. This constructor creates a blank session which should not be stored in the database.
    /// </summary>
    public LoggedInUser() : this(null, []) {}
}