using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using CentralBiro.Database;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Service;

public class LoginManager : ServiceClass
{
    protected override Dictionary<Tuple<string, string>, RequestDelegate> RequestDelegates { get; }

    private LoginManager()
    {
        RequestDelegates = new Dictionary<Tuple<string, string>, RequestDelegate>
        {
            { new Tuple<string, string>("POST", ""), LoginRequest}
        };
    }

    public static LoginManager Instance { get; } = new LoginManager();

    [Serializable, XmlRoot("LoginResponse")]
    public struct LoginResponse(bool success, byte[] token)
    {
        public bool Success = success;
        public byte[] Token = token;
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

    private byte[] GetToken(User user)
    {
        using var context = new CentralContext();
        LoggedInUser? loggedInUser = context.LoggedInUsers.FirstOrDefault(u => u.User == user);
        if (loggedInUser == null) loggedInUser = AddLoggedInUser(user, GenerateToken());
        else ExtendLoggedInUser(loggedInUser);
        return loggedInUser.Token;
    }

    private byte[] LoginRequest(HttpHandler.Request request, out int statusCode, out string contentType)
    {
        contentType = "text/xml";
        if (!request.Args.ContainsKey("username") || !request.Args.ContainsKey("password"))
        {
            statusCode = 400;
            return Serialize(new LoginResponse(false, "Arguments missing"u8.ToArray()));
        }
        
        using var context = new CentralContext();
        User? user = context.Users.AsNoTracking().FirstOrDefault(u => u.Username == request.Args["username"]);
        if (user == null)
        {
            statusCode = 404;
            return Serialize(new LoginResponse(false, "Username not found"u8.ToArray()));
        }
        
        byte[] password = CalculateHashedPassword(request.Args["password"], user.Salt);
        if (!user.Password.SequenceEqual(password))
        {
            statusCode = 404;
            return Serialize(new LoginResponse(false, "Wrong password"u8.ToArray()));
        }
        
        byte[] token = GetToken(user);
        statusCode = 200;
        return Serialize(new LoginResponse(true, token));
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
}