using System.Reflection;
using System.Xml.Serialization;
using CentralBiro;
using CentralBiro.Contract;
using CentralBiro.Database;
using CentralBiro.Service;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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
        LoginManager loginManager = new();
        bool actual = loginManager.AddUser(username, password);
        CentralContext context = new CentralContext();

        User[] users = context.Users.Where(user => user.Username == username).ToArray();
        
        Assert.IsTrue(actual); // Adding the user should be successful
        Assert.AreEqual(users.Length, 1); // There should only be one user, because the username is unique
        Assert.Greater(users[0].Salt.Length, 0); // The salt should not be empty
        Assert.Greater(users[0].Password.Length, 0); // The password should not be empty
        
        byte[] expectedPassword = loginManager.CalculateHashedPassword(password, users[0].Salt);
        CollectionAssert.AreEqual(expectedPassword, users[0].Password); // Checking if the password is properly set
    }

    [Test]
    [TestCase("test_1", "Test_1__abcdef")]
    [TestCase("testtwo", "Tsadf@536asf")]
    [TestCase("123", "Test_1__abcdef")]
    [TestCase("___", "Tsadf@536asf")]
    public void AddDuplicateUserTest(string username, string password)
    {
        LoginManager loginManager = new();
        loginManager.AddUser(username, password);
        Assert.Throws<DbUpdateException>(() => loginManager.AddUser(username, password)); // Making sure that two users cannot have the same username
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
        LoginManager loginManager = new();
        string password = "Test_1__abcdef";
        
        Assert.Throws<ArgumentException>(() => loginManager.AddUser(username, password));
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
        LoginManager loginManager = new();
        string username = "Test_123";
        
        Assert.Throws<ArgumentException>(() => loginManager.AddUser(username, password));
    }

    [Test]
    public void VerifyValidTokenTest()
    {
        LoginManager loginManager = new();
        string username = "Test_123";
        string password = "Test_1__abcdef";
        loginManager.AddUser(username, password);
        LoginResponse? response = (loginManager.LoginRequest(username, password) as ObjectResult)!.Value as LoginResponse?;
        
        bool actual = loginManager.Verify(response?.Token!);

        Assert.IsTrue(actual);
    }

    [Test]
    public void VerifyInvalidTokenTest()
    {
        LoginManager loginManager = new();
        byte[] token = "8994b6ff9963b65251b22bd8f251c12f03e6179f9c54205430aee4546a6d73d1"u8.ToArray();
        
        bool actual = loginManager.Verify(token);
        
        Assert.IsFalse(actual);
    }

    [Test]
    public void GetUsernameWithValidTokenTest()
    {
        LoginManager loginManager = new();
        string expectedUsername = "Test_123";
        string password = "Test_1__abcdef";
        loginManager.AddUser(expectedUsername, password);
        LoginResponse? response = (loginManager.LoginRequest(expectedUsername, password) as ObjectResult)!.Value as LoginResponse?;
        
        string? actual = loginManager.GetUsername(response?.Token!);
        
        Assert.AreEqual(expectedUsername, actual);
    }
    
    [Test]
    public void GetUsernameWithInvalidTokenTest()
    {
        LoginManager loginManager = new();
        byte[] token = "8994b6ff9963b65251b22bd8f251c12f03e6179f9c54205430aee4546a6d73d1"u8.ToArray();
        string? expectedUsername = null;
        
        string? actual = loginManager.GetUsername(token);
        
        Assert.AreEqual(expectedUsername, actual);
    }
    
    [Test]
    public void GetIdWithValidTokenTest()
    {
        LoginManager loginManager = new();
        string username = "Test_123";
        string password = "Test_1__abcdef";
        loginManager.AddUser(username, password);
        LoginResponse? response = (loginManager.LoginRequest(username, password) as ObjectResult)!.Value as LoginResponse?;
        
        int? actual = loginManager.GetId(response?.Token!);
        int expected = new CentralContext().Users.Single(u => u.Username == username).Id;
        
        Assert.AreEqual(expected, actual);
    }
    
    [Test]
    public void GetIdWithInvalidTokenTest()
    {
        LoginManager loginManager = new();
        byte[] token = "8994b6ff9963b65251b22bd8f251c12f03e6179f9c54205430aee4546a6d73d1"u8.ToArray();
        int? expected = null;
        
        int? actual = loginManager.GetId(token);
        
        Assert.AreEqual(expected, actual);
    }

    [Test]
    [TestCase("test_1", "Test_1__abcdef")]
    [TestCase("testtwo", "Tsadf@536asf")]
    [TestCase("123", "Test_1__abcdef")]
    [TestCase("___", "Tsadf@536asf")]
    public void ValidLoginRequestTest(string username, string password)
    {
        LoginManager loginManager = new();
        loginManager.AddUser(username, password);
        
        var result = loginManager.LoginRequest(username, password);
        
        Assert.IsInstanceOf<OkObjectResult>(result);
        Assert.IsInstanceOf<LoginResponse>((result as OkObjectResult)!.Value);
        LoginResponse? actual = (result as OkObjectResult)!.Value as LoginResponse?;
        Assert.Greater(actual?.Token.Length, 0);
        Assert.IsTrue(actual?.Success);
    }

    [Test]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void MissingArgumentLoginRequestTest(int testCase)
    {
        LoginManager loginManager = new();
        string username = "test_1";
        string password = "Test_1__abcdef";
        loginManager.AddUser(username, password);

        var result = testCase switch
        {
            1 => loginManager.LoginRequest(username: username),
            2 => loginManager.LoginRequest(password: password),
            3 => loginManager.LoginRequest(),
        };
        
        Assert.IsInstanceOf<BadRequestObjectResult>(result);
        Assert.IsInstanceOf<LoginResponse>((result as BadRequestObjectResult)!.Value);
        LoginResponse? actual = (result as BadRequestObjectResult)!.Value as LoginResponse?;
        Assert.IsFalse(actual?.Success);
    }
    
    [Test]
    public void MissingUserLoginRequestTest()
    {
        LoginManager loginManager = new();
        string username = "test_1";
        string password = "Test_1__abcdef";
        
        var result = loginManager.LoginRequest(username, password);
        
        Assert.IsInstanceOf<NotFoundObjectResult>(result);
        Assert.IsInstanceOf<LoginResponse>((result as NotFoundObjectResult)!.Value);
        LoginResponse? actual = (result as NotFoundObjectResult)!.Value as LoginResponse?;
        Assert.IsFalse(actual?.Success);
    }

    [Test]
    public void ConcurrentUserLoginRequestTest()
    {
        string username = "test_1";
        string password = "Test_1__abcdef";
        LoginManager loginManager = new();
        loginManager.AddUser(username, password);
        
        Task<IActionResult> login1 = Task.Run(() => loginManager.LoginRequest(username, password));
        Task<IActionResult> login2 = Task.Run(() => loginManager.LoginRequest(username, password));
        Task.WhenAll(login1, login2);
        LoginResponse? actual1 = (login1.Result as ObjectResult)!.Value as LoginResponse?;
        LoginResponse? actual2 = (login2.Result as ObjectResult)!.Value as LoginResponse?;
        
        Assert.IsTrue(actual1?.Success);
        Assert.IsTrue(actual2?.Success);
        Assert.AreEqual(actual1?.Token, actual2?.Token);
    }

    [SetUp]
    public void SetUp()
    {
        new CentralContext().Database.EnsureDeleted();
        new CentralContext().Database.EnsureCreated();
        
        //Removing any logged-in users
        FieldInfo loggedInUsers = typeof(LoginManager)
            .GetField("LoggedInUsers", BindingFlags.NonPublic | BindingFlags.Static)!;
        List<LoggedInUser> users = (List<LoggedInUser>) loggedInUsers.GetValue(null)!;
        users.Clear();
    }
}