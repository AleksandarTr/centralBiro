using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using CentralBiro.Contract;
using CentralBiro.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Service;

/// <summary>
/// <c>LoginManager</c> class is used to allow a user to login and subsequently verify that they are making requests
/// </summary>
[ApiController]
[Route("api/login")]
public class LoginManager : ControllerBase
{
    ///<value>
    /// <c>TokenDeletionActivation</c> is a constant indication the period in milliseconds
    /// of the <see cref="DeleteExpiredTokens"/> activation.
    /// </value>
    private const int TokenDeletionActivation = 5 * 60 * 1000;
    private static readonly Object Lock = new();
    
    static LoginManager() {
        //Thread which periodically removes expired tokens
        new Thread(() =>
        {
            while (true)
            {
                DeleteExpiredTokens();
                Thread.Sleep(TokenDeletionActivation);
            }
        }){IsBackground = true}.Start();
    }

    /// <summary>
    /// <c>DeleteExpiredTokens</c> is run periodically in a thread created in the constructor
    /// to delete any tokens that have expired.
    /// </summary>
    private static void DeleteExpiredTokens()
    {
        lock (Lock)
        {
            using var context = new CentralContext();
            context.LoggedInUsers.Where(user => DateTime.Now > user.Expiration).ExecuteDelete();
            context.SaveChanges();
        }
    }

    /// <summary>
    /// <c>CalculateHashedPassword</c> is used to calculate the hashed password stored in the database
    /// </summary>
    /// <param name="password">User's password in plaintext</param>
    /// <param name="salt">Salt that is stored alongside the hashed password in the user's data</param>
    /// <returns>The hashed password represented as a byte array</returns>
    public byte[] CalculateHashedPassword(string password, byte[] salt)
    {
        //Password is calculated with the pbkdf2 hashing algorithm using sha256
        Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, 5000, HashAlgorithmName.SHA256);
        byte[] result = pbkdf2.GetBytes(32);
        return result;
    }

    /// <summary>
    /// <c>AddLoggedInUser</c> is used to store a new <see cref="LoggedInUser"/> in the database.
    /// </summary>
    /// <param name="user">The user who has just logged in</param>
    /// <param name="token">The token generated for the user</param>
    /// <returns>An instance of <see cref="LoggedInUser"/> with the provided parameters</returns>
    private LoggedInUser AddLoggedInUser(User user, byte[] token)
    {
        LoggedInUser loggedInUser = new LoggedInUser(user, token);
        using var context = new CentralContext();
        context.Attach(user); //The user needs to be attached, because otherwise the context will try to add it,
                              //resulting in a unique constraint violation
        context.LoggedInUsers.Add(loggedInUser);
        context.SaveChanges();
        return loggedInUser;
    }
    
    /// <summary>
    /// <c>ExtendLoggedInUser</c> extends the expiration of a user's session to the point
    /// <see cref="LoggedInUser.LoginDuration"/> minutes from now.
    /// </summary>
    /// <param name="user">The user whose session is being extended</param>
    private void ExtendLoggedInUser(LoggedInUser user)
    {
        user.Expiration = DateTime.Now.AddMinutes(LoggedInUser.LoginDuration);
        using var context = new CentralContext();
        context.LoggedInUsers.Update(user);
        context.SaveChanges();
    }

    /// <summary>
    /// <c>GenerateToken</c> is used to generate a token used to verify a user's identity with their following requests
    /// </summary>
    /// <returns>A newly generated token for the user in the form of a byte array</returns>
    private byte[] GenerateToken()
    {
        long timestamp = DateTime.UtcNow.Ticks;
        SHA256 sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(timestamp.ToString()));
    }

    /// <summary>
    /// <c>GetToken</c> either: <list type="bullet">
    /// <item>fetches the token of an existing user</item>
    /// <item>generates a token and adds the record of the user's login to the database and
    /// returns the generated token</item>
    /// </list>
    /// </summary>
    /// <param name="user">The user whose token is being fetched</param>
    /// <returns>The user's token in the form of a byte array</returns>
    public byte[] GetToken(User user)
    {
        lock (Lock)
        {
            using var context = new CentralContext();
            LoggedInUser? loggedInUser = context.LoggedInUsers.FirstOrDefault(u => u.User == user);
            if (loggedInUser == null) loggedInUser = AddLoggedInUser(user, GenerateToken());
            else ExtendLoggedInUser(loggedInUser);
            return loggedInUser.Token;
        }
    }

    /// <summary>
    /// <c>Verify</c> tests if the token provided is associated with a logged-in user whose session has not expired
    /// </summary>
    /// <remarks><c>Verify</c> doesn´t itself test if the token has expired, so it is possible for the token to be
    /// marked valid, even after it has expired, but before the periodic cleanup has removed it.</remarks> 
    /// <param name="token"></param>
    /// <returns></returns>
    public bool Verify(byte[] token)
    {
        using var context = new CentralContext();
        LoggedInUser? loggedInUser = context.LoggedInUsers.Find(token);
        if (loggedInUser != null) lock(Lock) ExtendLoggedInUser(loggedInUser);
            //This method is called when a user sends a request so their session gets extended
        
        return loggedInUser != null;
    }

    /// <summary>
    /// <c>CheckUsername</c> checks if the provided username conforms to the rules for usernames.
    /// </summary>
    /// <param name="username">Username to be checked</param>
    /// <exception cref="ArgumentException">The username doesn´t conform to the rules </exception>
    /// <remarks>The username is checked only when the user is being added into the system.<br/>
    /// The rules, which might change, are as follows:
    /// <list type="bullet">
    /// <item>The username must be between 3 and 32 characters long</item>
    /// <item>The username may only contain letters, numbers and underscores</item>
    /// </list></remarks>
    private void CheckUsername(string username)
    {
        Regex correct = new Regex(@"^\w{3,32}$");
        if (correct.IsMatch(username)) return;
        
        Regex tooShort = new Regex(@"^\w{0,2}$");
        if (tooShort.IsMatch(username)) throw new ArgumentException("Username must be at least 3 characters");
        Regex tooLong = new Regex(@"\w{33,}");
        if (tooLong.IsMatch(username)) throw new ArgumentException("Username must be at most 32 characters");
        throw new ArgumentException("Username can only contain letters, numbers, and underscores.");
    }

    /// <summary>
    /// <c>CheckPassword</c> checks if the provided password conforms to the rules for passwords.
    /// </summary>
    /// <param name="password">Password to be checked</param>
    /// <exception cref="ArgumentException">The password doesn´t conform to the rules </exception>
    /// <remarks>The password is checked only when the user is being added into the system.<br/>
    /// The rules, which might change, are as follows:
    /// <list type="bullet">
    /// <item>The password must be between 12 and 64 characters long</item>
    /// <item>The password may contain any character</item>
    /// <item>The password must contain at least one lowercase letter</item>
    /// <item>The password must contain at least one uppercase letter</item>
    /// <item>The password must contain at least one digit</item>
    /// <item>The password must contain at least one special character</item>
    /// </list></remarks>
    private void CheckPassword(string password)
    {
        Regex tooShort = new Regex(@"^.{0,11}$");
        if(tooShort.IsMatch(password)) throw new ArgumentException("Password must be at least 12 characters");
        Regex tooLong = new Regex(@"^.{65,}$");
        if(tooLong.IsMatch(password)) throw new ArgumentException("Password must be at most 64 characters");
        Regex hasLowercase = new Regex(@"[a-z]");
        if(!hasLowercase.IsMatch(password)) throw new ArgumentException("Password must contain at least one lowercase letter");
        Regex hasUppercase = new Regex(@"[A-Z]");
        if(!hasUppercase.IsMatch(password)) throw new ArgumentException("Password must contain at most one uppercase letter");
        Regex hasNumber = new Regex(@"[0-9]");
        if(!hasNumber.IsMatch(password)) throw new ArgumentException("Password must contain at most one number");
        Regex hasSpecialChar = new Regex(@"[^a-zA-Z0-9]");
        if(!hasSpecialChar.IsMatch(password)) throw new ArgumentException("Password must contain at most one special character");
    }

    /// <summary>
    /// <c>AddUser</c> adds a user to the database
    /// </summary>
    /// <param name="username">User's username</param>
    /// <param name="password">User's password in plaintext</param>
    /// <returns>Bool indicating if the user has been added successfully</returns>
    /// <exception cref="ArgumentException">The user's username/password doesn´t
    /// follow their respective naming rules</exception>
    /// <exception cref="DbUpdateException">A problem caused by inserting the user record into the database,
    /// most likely because the username is already present in the database</exception>
    /// <remarks>This method will only ever be called locally</remarks>
    public bool AddUser(string username, string password)
    {
        //Throw exceptions in case of username/password naming violations 
        CheckUsername(username);
        CheckPassword(password);
        
        byte[] salt = GenerateToken();
        byte[] passwordHash = CalculateHashedPassword(password, salt);
        User user = new User(username, passwordHash, salt);
        
        using var context = new CentralContext();
        context.Users.Add(user);
        context.SaveChanges();

        return true;
    }

    /// <summary>
    /// <c>GetUsername</c> fetches the username of a logged-in user
    /// </summary>
    /// <param name="token">User's non-expired token</param>
    /// <returns>The user's username if their token is valid, otherwise null</returns>
    public string? GetUsername(byte[] token)
    {
        using var context = new CentralContext();
        LoggedInUser? loggedInUser = context.LoggedInUsers.Include(user => user.User).FirstOrDefault(user => user.Token == token);
        return loggedInUser?.User.Username;
    }

    /// <summary>
    /// <c>GetId</c> fetches the id of a logged-in user
    /// </summary>
    /// <param name="token">User's non-expired token</param>
    /// <returns>The user's id if their token is valid, otherwise null</returns>
    public int? GetId(byte[] token)
    {
        using var context = new CentralContext();
        LoggedInUser? loggedInUser = context.LoggedInUsers.Include(user => user.User).FirstOrDefault(user => user.Token == token);
        return loggedInUser?.User.Id;
    }

    /// <summary>
    /// <c>GetUser</c> fetches an instance of the <see cref="User"/> who is associated with the token
    /// </summary>
    /// <param name="token">User's non-expired token</param>
    /// <returns>A <see cref="User"/> instance if their token is valid, otherwise null</returns>
    public User? GetUser(byte[] token)
    {
        using var context = new CentralContext();
        LoggedInUser? loggedInUser = context.LoggedInUsers.Include(user => user.User).FirstOrDefault(user => user.Token == token);
        return loggedInUser?.User;
    }
    
    /// <summary>
    /// <c>LoginRequest</c> processes a login request and generates a token if it is valid or fetches an existing token
    /// </summary>
    /// <param name="username">User's username</param>
    /// <param name="password">User's password in plaintext</param>
    /// <returns><see cref="LoginResponse"/> where:<list type="bullet">
    /// <item><see cref="LoginResponse.Success"/> is true if the request is valid and
    /// <see cref="LoginResponse.Token"/> then contains a valid token associated with the user</item>
    /// <item><see cref="LoginResponse.Success"/> is false if the request is invalid and
    /// <see cref="LoginResponse.Token"/> may contain any data in that case </item>
    /// </list></returns>
    [HttpPost]
    public IActionResult LoginRequest([FromForm] string? username = null, [FromForm] string? password = null)
    {
        //Check if all the arguments have been provided
        if (username == null)
            return BadRequest(new LoginResponse(false, "Username not provided"u8.ToArray()));

        if (password == null)
            return BadRequest(new LoginResponse(false, "Password not provided"u8.ToArray()));

        //Check if the username exists in the database
        using var context = new CentralContext();
        User? user = context.Users.AsNoTracking().FirstOrDefault(u => u.Username == username);
        if (user == null)
            return NotFound(new LoginResponse(false, "No user with provided username/password combination"u8.ToArray()));
        
        //Check if the password matches with the user's database record
        byte[] hashedPassword = CalculateHashedPassword(password, user.Salt);
        if (!user.Password.SequenceEqual(hashedPassword))
            return NotFound(new LoginResponse(false, "No user with provided username/password combination"u8.ToArray()));

        //Fetch a logged-in user's token/generate a new token
        byte[] token = GetToken(user);
        return Ok(new LoginResponse(true, token));
    }
}