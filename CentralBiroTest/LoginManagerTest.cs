using System.Xml.Serialization;
using CentralBiro;
using CentralBiro.Database;
using CentralBiro.Service;
using Microsoft.EntityFrameworkCore;

namespace CentralBiroTest;

public class LoginManagerTest
{
    [Test]
    [TestCase("test_1", "Test_1__abcdef")]
    [TestCase("testtwo", "Tsadf@536asf")]
    [TestCase("123", "Test_1__abcdef")]
    [TestCase("___", "Tsadf@536asf")]
    public void AddUserTest(string username, string password)
    {
        bool actual = LoginManager.Instance.AddUser(username, password);
        CentralContext context = new CentralContext();

        User[] users = context.Users.Where(user => user.Username == username).ToArray();
        
        Assert.IsTrue(actual); // Adding the user should be successful
        Assert.AreEqual(users.Length, 1); // There should only be one user, because the username is unique
        Assert.Greater(users[0].Salt.Length, 0); // The salt should not be empty
        Assert.Greater(users[0].Password.Length, 0); // The password should not be empty
        
        byte[] expectedPassword = LoginManager.Instance.CalculateHashedPassword(password, users[0].Salt);
        CollectionAssert.AreEqual(expectedPassword, users[0].Password); // Checking if the password is properly set
    }

    [Test]
    [TestCase("test_1", "Test_1__abcdef")]
    [TestCase("testtwo", "Tsadf@536asf")]
    [TestCase("123", "Test_1__abcdef")]
    [TestCase("___", "Tsadf@536asf")]
    public void AddDuplicateUserTest(string username, string password)
    {
        LoginManager.Instance.AddUser(username, password);
        Assert.Throws<DbUpdateException>(() => LoginManager.Instance.AddUser(username, password)); // Making sure that two users cannot have the same username
    }

    [Test]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("ab")]
    [TestCase("12")]
    [TestCase("__")]
    [TestCase("abcc!")]
    [TestCase("63a6 avz")]
    [TestCase("szgoij^")]
    public void AddUserWithInvalidUsernameTest(string username)
    {
        string password = "Test_1__abcdef";
        
        Assert.Throws<ArgumentException>(() => LoginManager.Instance.AddUser(username, password));
    }

    [Test]
    [TestCase("")]
    [TestCase("Ab12_")]
    [TestCase("Ab12_ahehpd")]
    [TestCase("Ab12_ahehpdfodiekvod;wlbiaq3-'.2-go1`azgof;wlv[wokafṕokasdfokaspf")]
    [TestCase("ABCDEFGHIJKLMN")]
    [TestCase("asbsdsdgfssfdg")]
    [TestCase("54461548468456")]
    [TestCase("$#⁾(*%#)*)$#)(%#")]
    [TestCase("Ahdnpok34535")]
    public void AddUserWithInvalidPasswordTest(string password)
    {
        string username = "Test_123";
        
        Assert.Throws<ArgumentException>(() => LoginManager.Instance.AddUser(username, password));
    }

    [Test]
    public void VerifyValidTokenTest()
    {
        byte[] token = "8994b6ff9963b65251b22bd8f251c12f03e6179f9c54205430aee4546a6d73d1"u8.ToArray();
        User user = new User("Test1234", new byte[] { }, new byte[] { });
        LoggedInUser prev = new LoggedInUser(user, token);
        using (CentralContext context = new CentralContext())
        {
            context.Users.Add(user);
            context.LoggedInUsers.Add(prev);
            context.SaveChanges();
        }
        Thread.Sleep(10);
        
        bool actual = LoginManager.Instance.Verify(token);
        LoggedInUser curr;
        using (CentralContext context = new CentralContext())
        {
            curr = context.LoggedInUsers.Find(token)!;
        }

        Assert.IsTrue(actual);
        Assert.Greater(curr.Expiration, prev.Expiration);
    }

    [Test]
    public void VerifyInvalidTokenTest()
    {
        byte[] token = "8994b6ff9963b65251b22bd8f251c12f03e6179f9c54205430aee4546a6d73d1"u8.ToArray();
        
        bool actual = LoginManager.Instance.Verify(token);
        
        Assert.IsFalse(actual);
    }

    [Test]
    public void GetUsernameWithValidTokenTest()
    {
        byte[] token = "8994b6ff9963b65251b22bd8f251c12f03e6179f9c54205430aee4546a6d73d1"u8.ToArray();
        string expectedUsername = "Test_123";
        CentralContext context = new CentralContext();
        User user = new User(expectedUsername, new byte[] { }, new byte[] { });
        context.Users.Add(user);
        context.SaveChanges();
        context.LoggedInUsers.Add(new LoggedInUser(user, token));
        context.SaveChanges();
        
        string actual = LoginManager.Instance.GetUsername(token);
        
        Assert.AreEqual(expectedUsername, actual);
    }
    
    [Test]
    public void GetUsernameWithInvalidTokenTest()
    {
        byte[] token = "8994b6ff9963b65251b22bd8f251c12f03e6179f9c54205430aee4546a6d73d1"u8.ToArray();
        string? expectedUsername = null;
        
        string? actual = LoginManager.Instance.GetUsername(token);
        
        Assert.AreEqual(expectedUsername, actual);
    }
    
    [Test]
    public void GetIdWithValidTokenTest()
    {
        byte[] token = "8994b6ff9963b65251b22bd8f251c12f03e6179f9c54205430aee4546a6d73d1"u8.ToArray();
        CentralContext context = new CentralContext();
        User user = new User("test", new byte[] { }, new byte[] { });
        context.Users.Add(user);
        context.SaveChanges();
        int expected = user.Id;
        context.LoggedInUsers.Add(new LoggedInUser(user, token));
        context.SaveChanges();
        
        int? actual = LoginManager.Instance.GetId(token);
        
        Assert.AreEqual(expected, actual);
    }
    
    [Test]
    public void GetIdWithInvalidTokenTest()
    {
        byte[] token = "8994b6ff9963b65251b22bd8f251c12f03e6179f9c54205430aee4546a6d73d1"u8.ToArray();
        int? expected = null;
        
        int? actual = LoginManager.Instance.GetId(token);
        
        Assert.AreEqual(expected, actual);
    }

    [Test]
    [TestCase("test_1", "Test_1__abcdef")]
    [TestCase("testtwo", "Tsadf@536asf")]
    [TestCase("123", "Test_1__abcdef")]
    [TestCase("___", "Tsadf@536asf")]
    public void ValidLoginRequestTest(string username, string password)
    {
        LoginManager.Instance.AddUser(username, password);
        HttpHandler.Request request = new HttpHandler.Request()
        {
            Args = new Dictionary<string, string>() { {"username", username}, {"password", password} },
            HttpMethod = "POST",
            ResourceUrl = "/login"
        };
        int statusCode;
        string contentType;
        
        XmlSerializer serializer = new XmlSerializer(typeof(LoginManager.LoginResponse));
        byte[] result = LoginManager.Instance.Execute(request, out statusCode, out contentType);
        LoginManager.LoginResponse actual = (LoginManager.LoginResponse)serializer.Deserialize(new MemoryStream(result))!;
        
        Assert.IsTrue(actual.Success);
        Assert.Greater(actual.Token.Length, 0);
        Assert.AreEqual(statusCode, 200);
        Assert.AreEqual(contentType, "text/xml");
    }

    [Test]
    [TestCase("username")]
    [TestCase("password")]
    public void MissingArgumentLoginRequestTest(string key)
    {
        string username = "test_1";
        string password = "Test_1__abcdef";
        LoginManager.Instance.AddUser(username, password);
        
        HttpHandler.Request request = new HttpHandler.Request()
        {
            Args = new Dictionary<string, string>() { {"username", username}, {"password", password} },
            HttpMethod = "POST",
            ResourceUrl = "/login"
        };
        int statusCode;
        string contentType;
        request.Args.Remove(key);
        
        XmlSerializer serializer = new XmlSerializer(typeof(LoginManager.LoginResponse));
        byte[] result = LoginManager.Instance.Execute(request, out statusCode, out contentType);
        LoginManager.LoginResponse actual = (LoginManager.LoginResponse)serializer.Deserialize(new MemoryStream(result))!;
        
        Assert.IsFalse(actual.Success);
        Assert.AreEqual(statusCode, 400);
        Assert.AreEqual(contentType, "text/xml");
    }
    
    [Test]
    public void MissingUserLoginRequestTest()
    {
        string username = "test_1";
        string password = "Test_1__abcdef";
        HttpHandler.Request request = new HttpHandler.Request()
        {
            Args = new Dictionary<string, string>() { {"username", username}, {"password", password} },
            HttpMethod = "POST",
            ResourceUrl = "/login"
        };
        int statusCode;
        string contentType;
        
        XmlSerializer serializer = new XmlSerializer(typeof(LoginManager.LoginResponse));
        byte[] result = LoginManager.Instance.Execute(request, out statusCode, out contentType);
        LoginManager.LoginResponse actual = (LoginManager.LoginResponse)serializer.Deserialize(new MemoryStream(result))!;
        
        Assert.IsFalse(actual.Success);
        Assert.AreEqual(statusCode, 404);
        Assert.AreEqual(contentType, "text/xml");
    }

    [Test]
    [TestCase("POST", "/login/apple")]
    [TestCase("GET", "/login")]
    [TestCase("DELETE", "/login/dot/net")]
    [TestCase("", "/login")]
    public void BadLoginRequestTest(string httpMethod, string resourceUrl)
    {
        string username = "test_1";
        string password = "Test_1__abcdef";
        LoginManager.Instance.AddUser(username, password);
        
        HttpHandler.Request request = new HttpHandler.Request()
        {
            Args = new Dictionary<string, string>() { {"username", username}, {"password", password} },
            HttpMethod = httpMethod,
            ResourceUrl = resourceUrl
        };
        int statusCode;
        string contentType;
        
        byte[] actual = LoginManager.Instance.Execute(request, out statusCode, out contentType);
        
        Assert.AreEqual(statusCode, 404);
        Assert.AreEqual(contentType, "text/plain");
    }

    [SetUp]
    public void SetUp()
    {
        new CentralContext().Database.EnsureDeleted();
    }
}