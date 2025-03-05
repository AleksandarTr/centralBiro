using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using CentralBiro.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Service;

public struct LoginResponse(bool success, byte[] token)
{
    public bool Success { get; set; } = success;
    public byte[] Token { get; set; } = token;
}

public class LoginManager
{
    private LoginManager()
    {
        // new Thread(() =>
        // {
        //     while (true)
        //     {
        //         DeleteExpiredTokens();
        //         Thread.Sleep(5 * 60 * 1000);
        //     }
        // }){IsBackground = true}.Start();
    }

    public static LoginManager Instance { get; } = new LoginManager();

    private void DeleteExpiredTokens()
    {
        using var context = new CentralContext();
        context.LoggedInUsers.Where(user => DateTime.Now > user.Expiration).ExecuteDelete();
        context.SaveChanges();
    }

    public byte[] CalculateHashedPassword(string password, byte[] salt)
    {
        Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, 5000, HashAlgorithmName.SHA256);
        byte[] result = pbkdf2.GetBytes(32);
        return result;
    }

    private LoggedInUser AddLoggedInUser(User user, byte[] token)
    {
        LoggedInUser loggedInUser = new LoggedInUser(user, token);
        using var context = new CentralContext();
        context.Attach(user);
        context.LoggedInUsers.Add(loggedInUser);
        context.SaveChanges();
        return loggedInUser;
    }
    
    private void ExtendLoggedInUser(LoggedInUser user)
    {
        user.Expiration = DateTime.Now.AddMinutes(LoggedInUser.LoginDuration);
        using var context = new CentralContext();
        context.LoggedInUsers.Update(user);
        context.SaveChanges();
    }

    private byte[] GenerateToken()
    {
        long timestamp = DateTime.UtcNow.Ticks;
        SHA256 sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(timestamp.ToString()));
    }

    public byte[] GetToken(User user)
    {
        using var context = new CentralContext();
        LoggedInUser? loggedInUser = context.LoggedInUsers.FirstOrDefault(u => u.User == user);
        if (loggedInUser == null) loggedInUser = AddLoggedInUser(user, GenerateToken());
        else ExtendLoggedInUser(loggedInUser);
        return loggedInUser.Token;
    }

    public bool Verify(byte[] token)
    {
        using var context = new CentralContext();
        LoggedInUser? loggedInUser = context.LoggedInUsers.Find(token);
        if (loggedInUser != null) ExtendLoggedInUser(loggedInUser);
        return loggedInUser != null;
    }

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

    public bool AddUser(string username, string password)
    {
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

    public string? GetUsername(byte[] token)
    {
        using var context = new CentralContext();
        LoggedInUser? loggedInUser = context.LoggedInUsers.Include(user => user.User).FirstOrDefault(user => user.Token == token);
        return loggedInUser?.User.Username;
    }

    public int? GetId(byte[] token)
    {
        using var context = new CentralContext();
        LoggedInUser? loggedInUser = context.LoggedInUsers.Include(user => user.User).FirstOrDefault(user => user.Token == token);
        return loggedInUser?.User.Id;
    }

    public User? GetUser(byte[] token)
    {
        using var context = new CentralContext();
        LoggedInUser? loggedInUser = context.LoggedInUsers.Include(user => user.User).FirstOrDefault(user => user.Token == token);
        return loggedInUser?.User;
    }
}

[ApiController]
[Route("api/login")]
public class LoginController : ControllerBase
{
    [HttpPost]
    public IActionResult LoginRequest([FromForm] string? username = null, [FromForm] string? password = null)
    {
        Console.WriteLine($"Login request: {username}, {password}");
        if (username == null)
            return BadRequest(new LoginResponse(false, "Username not provided"u8.ToArray()));

        if (password == null)
            return BadRequest(new LoginResponse(false, "Password not provided"u8.ToArray()));

        using var context = new CentralContext();
        User? user = context.Users.AsNoTracking().FirstOrDefault(u => u.Username == username);
        if (user == null)
            return NotFound(new LoginResponse(false, "No user with provided username/password combination"u8.ToArray()));


        byte[] hashedPassword = LoginManager.Instance.CalculateHashedPassword(password, user.Salt);
        if (!user.Password.SequenceEqual(hashedPassword))
            return NotFound(new LoginResponse(false, "No user with provided username/password combination"u8.ToArray()));

        byte[] token = LoginManager.Instance.GetToken(user);
        return Ok(new LoginResponse(true, token));
    }
}