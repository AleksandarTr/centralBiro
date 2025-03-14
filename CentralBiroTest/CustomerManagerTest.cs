using System.Reflection;
using CentralBiro.Contract;
using CentralBiro.Database;
using CentralBiro.Service;
using Microsoft.AspNetCore.Mvc;
using Tmds.DBus.Protocol;

namespace CentralBiroTest;

public class CustomerManagerTest
{
    private byte[] _token = [];
    
    [SetUp]
    public void Setup()
    {
        new CentralContext().Database.EnsureDeleted();
        new CentralContext().Database.EnsureCreated();
        
        //Removing any logged-in users
        FieldInfo loggedInUsers = typeof(LoginManager)
            .GetField("LoggedInUsers", BindingFlags.NonPublic | BindingFlags.Static)!;
        List<LoggedInUser> users = (List<LoggedInUser>) loggedInUsers.GetValue(null)!;
        users.Clear();

        //Logging in
        string username = "test";
        string password = "Test____1234";
        LoginManager loginManager = new LoginManager();
        loginManager.AddUser(username, password);
        LoginResponse? response = (loginManager.LoginRequest(username, password) as ObjectResult)!.Value as LoginResponse?;
        _token = response!.Value.Token;
    }
    
    [Test]
    public void CreateRequestTest()
    {
        string name = "John Doe";
        string address = "123 Main Street";

        ObjectResult? reply = new CustomerManager()
            .CreateRequest(new([], [name, address], new GenericStatus(), _token)) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        
        using var context = new CentralContext();
        Customer? actual1 = context.Customers.SingleOrDefault(customer => customer.Name == name);
        Customer? actual2 = context.Customers.SingleOrDefault(customer => customer.Address == address);
        
        Assert.That(response?.Count, Is.GreaterThan(-1));
        Assert.That(actual1, Is.Not.Null);
        Assert.That(actual2, Is.Not.Null);
        Assert.That(reply.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public void UnauthorizedCreateRequestTest()
    {
        string name = "John Doe";
        string address = "123 Main Street";
        
        ObjectResult? reply = new CustomerManager()
            .CreateRequest(new([], [name, address], new GenericStatus(), [])) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        
        Assert.That(response?.Count, Is.EqualTo(-1));
        Assert.That(reply?.StatusCode, Is.EqualTo(401));
    }

    [Test]
    [TestCase("John Doe", "")]
    [TestCase("", "123 Main Street")]
    [TestCase("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxy", "123 Main Street")]
    [TestCase("John Doe", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvw")]
    public void BadParamCreateRequestTest(string name, string address)
    {
        ObjectResult? reply = new CustomerManager()
            .CreateRequest(new([], [name, address], new GenericStatus(), _token)) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        
        using var context = new CentralContext();
        Customer? actual1 = context.Customers.SingleOrDefault(customer => customer.Name == name);
        Customer? actual2 = context.Customers.SingleOrDefault(customer => customer.Address == address);
        
        Assert.That(response?.Count, Is.EqualTo(-1));
        Assert.That(actual1, Is.Null);
        Assert.That(actual2, Is.Null);
        Assert.That(reply.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public void ConcurrentCreateRequestTest()
    {
        string name = "John Doe";
        string address = "123 Main Street";

        Task<IActionResult> task1 = Task.Run(() => new CustomerManager()
            .CreateRequest(new([], [name, address], new GenericStatus(), _token)));
        Task<IActionResult> task2 = Task.Run(() => new CustomerManager()
            .CreateRequest(new([], [name, address], new GenericStatus(), _token)));
        
        CrudResponse? response1 = (task1.Result as ObjectResult)!.Value as CrudResponse?;
        CrudResponse? response2 = (task2.Result as ObjectResult)!.Value as CrudResponse?;
        
        Assert.That(response1?.Count, Is.Not.EqualTo(response2?.Count));
        Assert.IsTrue(response1?.Count > -1 != response2?.Count > -1);
    }

    [Test]
    public void DuplicateCreateRequestTest()
    {
        string name = "John Doe";
        string address = "123 Main Street";
        
        CustomerManager customerManager = new CustomerManager();

        customerManager.CreateRequest(new([], [name, address], new GenericStatus(), _token));
        ObjectResult? reply = customerManager
            .CreateRequest(new([], [name, address], new GenericStatus(), _token)) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        
        Assert.That(response?.Count, Is.EqualTo(-1));
        Assert.That(reply.StatusCode, Is.EqualTo(400));
    }

    [Test]
    [TestCase(new int[] {1, Customer.FirstId}, new string[] {})]
    [TestCase(new int[] {2}, new string[] {"John Doe"})]
    [TestCase(new int[] {3}, new string[] {"123 Main Street"})]
    public void ReadRequestTest(int[] intParams, string[] stringParams)
    {
        string name = "John Doe";
        string address = "123 Main Street";
        CustomerManager customerManager = new CustomerManager();
        customerManager.CreateRequest(new([], [name, address], new GenericStatus(), _token));
        using var context = new CentralContext();
        Customer customer = context.Customers.Single(customer => customer.Name == name);
        
        ObjectResult? reply = customerManager
            .ReadRequest(new(intParams, stringParams, new GenericStatus(), _token)) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        
        Assert.That(response?.Count, Is.EqualTo(1));
        Assert.That(response?.Result, Is.InstanceOf<Customer[]>());
        Assert.That((response?.Result as Customer[])![0].Id, Is.EqualTo(customer.Id));
        Assert.That(reply.StatusCode, Is.EqualTo(200));
    }

    [Test]
    [TestCase(new int[] {2}, new string[] {"john"})]
    [TestCase(new int[] {3}, new string[] {"123"})]
    public void MultiInstanceReadRequestTest(int[] intParams, string[] stringParams)
    {
        string name1 = "John Doe";
        string address1 = "123 Main Street";
        string name2 = "John Boe";
        string address2 = "123 Side Street";
        
        CustomerManager customerManager = new CustomerManager();
        customerManager.CreateRequest(new([], [name1, address1], new GenericStatus(), _token));
        customerManager.CreateRequest(new([], [name2, address2], new GenericStatus(), _token));
        using var context = new CentralContext();
        Customer customer1 = context.Customers.Single(customer => customer.Name == name1);
        Customer customer2 = context.Customers.Single(customer => customer.Name == name2);
        
        ObjectResult? reply = customerManager
            .ReadRequest(new(intParams, stringParams, new GenericStatus(), _token)) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        
        Assert.That(response?.Count, Is.EqualTo(2));
        Assert.That(response?.Result, Is.InstanceOf<Customer[]>());
        Assert.That((response?.Result as Customer[])!.SingleOrDefault(customer => customer.Id == customer1.Id), Is.Not.Null);
        Assert.That((response?.Result as Customer[])!.SingleOrDefault(customer => customer.Id == customer2.Id), Is.Not.Null);
        Assert.That(reply.StatusCode, Is.EqualTo(200));
    }

    [Test]
    [TestCase(new int[] { 1, Customer.FirstId }, new string[] { })]
    [TestCase(new int[] { 2 }, new string[] { "John Doe" })]
    [TestCase(new int[] { 3 }, new string[] { "123 Main Street" })]
    public void NoInstanceReadRequestTest(int[] intParams, string[] stringParams)
    {
        CustomerManager customerManager = new CustomerManager();
        
        ObjectResult? reply = customerManager
            .ReadRequest(new(intParams, stringParams, new GenericStatus(), _token)) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        
        Assert.That(response?.Count, Is.EqualTo(0));
        Assert.That(response?.Result, Is.InstanceOf<Customer[]>());
        Assert.That(reply.StatusCode, Is.EqualTo(200));
    }

    [Test]
    [TestCase(new int[] { }, new string[] { "John Doe" })]
    [TestCase(new int[] { 0, 1 }, new string[] { "John Doe" })]
    [TestCase(new int[] { 1 }, new string[] { })]
    [TestCase(new int[] { 2 }, new string[] { })]
    [TestCase(new int[] { 3 }, new string[] { })]
    [TestCase(new int[] { 4, 1 }, new string[] { "John Doe" })]
    public void InvalidReadRequestTest(int[] intParams, string[] stringParams)
    {
        CustomerManager customerManager = new CustomerManager();
        
        ObjectResult? reply = customerManager
            .ReadRequest(new(intParams, stringParams, new GenericStatus(), _token)) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        
        Assert.That(response?.Count, Is.EqualTo(-1));
        Assert.That(reply.StatusCode, Is.EqualTo(400));
    }
    
    [Test]
    public void UnauthorizedReadRequestTest()
    {
        string name = "John Doe";
        string address = "123 Main Street";
        CustomerManager customerManager = new();
        customerManager.CreateRequest(new ([], [name, address], new GenericStatus(), _token));
        
        ObjectResult? reply = customerManager
            .ReadRequest(new CrudRequest([1, Customer.FirstId], [], new GenericStatus(), [])) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        
        Assert.That(response?.Count, Is.EqualTo(-1));
        Assert.That(reply?.StatusCode, Is.EqualTo(401));
    }

    [Test]
    public void UpdateRequestTest()
    {
        string name1 = "John Doe";
        string address1 = "123 Main Street";
        string name2 = "John Boe";
        string address2 = "123 Side Street";
        
        CustomerManager customerManager = new();
        customerManager.CreateRequest(new([], [name1, address1], new GenericStatus(), _token));
        
        ObjectResult? reply = customerManager
            .UpdateRequest(new([Customer.FirstId], [name2, address2], new GenericStatus(), _token)) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        Customer actual = (((customerManager
            .ReadRequest(new([1, Customer.FirstId], [], new GenericStatus(), _token))
            as ObjectResult)!.Value as CrudResponse?)?.Result as Customer[])![0];
        
        Assert.That(response?.Count, Is.EqualTo(1));
        Assert.That(reply.StatusCode, Is.EqualTo(200));
        Assert.That(actual.Name, Is.EqualTo(name2));
        Assert.That(actual.Address, Is.EqualTo(address2));
    }

    [Test]
    [TestCase(new int[] {  }, new string[] { "John Boe", "123 Side Street" })]
    [TestCase(new int[] { Customer.FirstId }, new string[] { "John Boe" })]
    [TestCase(new int[] { Customer.FirstId }, new string[] { })]
    public void InvalidUpdateRequestTest(int[] intParams, string[] stringParams)
    {
        string name1 = "John Doe";
        string address1 = "123 Main Street";
        
        CustomerManager customerManager = new();
        customerManager.CreateRequest(new([], [name1, address1], new GenericStatus(), _token));
        
        ObjectResult? reply = customerManager
            .UpdateRequest(new(intParams, stringParams, new GenericStatus(), _token)) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        
        Assert.That(response?.Count, Is.EqualTo(-1));
        Assert.That(reply.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public void NonExistingCustomerUpdateRequestTest()
    {
        string name2 = "John Boe";
        string address2 = "123 Side Street";
        
        ObjectResult? reply = new CustomerManager()
            .UpdateRequest(new([Customer.FirstId], [name2, address2], new GenericStatus(), _token)) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        
        Assert.That(response?.Count, Is.EqualTo(-1));
        Assert.That(reply.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public void UnauthorizedUpdateRequestTest()
    {
        string name1 = "John Doe";
        string address1 = "123 Main Street";
        string name2 = "John Boe";
        string address2 = "123 Side Street";
        
        CustomerManager customerManager = new();
        customerManager.CreateRequest(new([], [name1, address1], new GenericStatus(), _token));
        
        ObjectResult? reply = customerManager
            .UpdateRequest(new([Customer.FirstId], [name2, address2], new GenericStatus(), [])) as ObjectResult;
        CrudResponse? response = reply!.Value as CrudResponse?;
        
        Assert.That(response?.Count, Is.EqualTo(-1));
        Assert.That(reply.StatusCode, Is.EqualTo(401));
    }
    
    //TODO: Create delete tests when ProductManager is implemented
}