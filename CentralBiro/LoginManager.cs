using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro;

public class LoginManager : ServiceClass
{
    protected override Dictionary<Tuple<string, string>, RequestDelegate> RequestDelegates { get; }
    private CentralContext _context = new CentralContext();

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

    private byte[] CalculateHashedPassword(string password, byte[] salt)
    {
        Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, 5000, HashAlgorithmName.SHA256);
        byte[] result = pbkdf2.GetBytes(32);
        return result;
    }

    private LoggedInUser AddLoggedInUser(User user, byte[] token)
    {
        LoggedInUser loggedInUser = new LoggedInUser(user, token);
        _context.LoggedInUsers.Add(loggedInUser);
        _context.SaveChanges();
        return loggedInUser;
    }
    
    private void ExtendLoggedInUser(LoggedInUser user)
    {
        user.Expiration = DateTime.Now.AddMinutes(LoggedInUser.LoginDuration);
        _context.LoggedInUsers.Update(user);
        _context.SaveChanges();
    }

    private byte[] GenerateToken()
    {
        long timestamp = DateTime.UtcNow.Ticks;
        SHA256 sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(timestamp.ToString()));
    }

    private byte[] GetToken(User user)
    {
        LoggedInUser? loggedInUser = _context.LoggedInUsers.Where(u => u.User == user).FirstOrDefault();
        if (loggedInUser == null) loggedInUser = AddLoggedInUser(user, GenerateToken());
        else ExtendLoggedInUser(loggedInUser);
        return loggedInUser.Token;
    }

    private byte[] LoginRequest(HttpHandler.Request request, out int statusCode, out string contentType)
    {
        statusCode = 200;
        contentType = "text/plain";
        
        User? user = _context.Users.Where(u => u.Username == request.Args["username"]).FirstOrDefault();
        if (user == null) return Serialize(new LoginResponse(false, "Username not found"u8.ToArray()));
        
        byte[] password = CalculateHashedPassword(request.Args["password"], user.Salt);
        if (!user.Password.SequenceEqual(password)) return Serialize(new LoginResponse(false, "Wrong password"u8.ToArray()));
        
        byte[] token = GetToken(user);
        return Serialize(new LoginResponse(true, token));
    }

    public bool Verify(byte[] token)
    {
        LoggedInUser? loggedInUser = _context.LoggedInUsers.Find(token);
        if (loggedInUser != null) ExtendLoggedInUser(loggedInUser);
        return loggedInUser != null;
    }

    public bool AddUser(string username, string password)
    {
        byte[] salt = GenerateToken();
        byte[] passwordHash = CalculateHashedPassword(password, salt);
        User user = new User(username, passwordHash, salt);
        try
        {
            _context.Users.Add(user);
            _context.SaveChanges();
            return true;
        }
        catch (DbUpdateException e)
        {
            return false;
        }
    }

    public string GetUsername(byte[] token)
    {
        LoggedInUser? loggedInUser = _context.LoggedInUsers.Find(token);
        if (loggedInUser == null) return "";
        return loggedInUser.User.Username;
    }

    public int GetId(byte[] token)
    {
        LoggedInUser? loggedInUser = _context.LoggedInUsers.Find(token);
        if (loggedInUser == null) return -1;
        return loggedInUser.User.Id;
    }
}