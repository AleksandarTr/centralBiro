using CentralBiro.Contract;
using CentralBiro.Database;
using CentralBiro.Service;
using Microsoft.AspNetCore.Mvc;

namespace CentralBiroTest;

public class ProductManagerTest
{
    // [Test]
    // [TestCase(1)]
    // public void ValidProductReservationRequest(int type)
    // {
    //     string username = "Test";
    //     string password = "Test1234!!!!";
    //     LoginManager loginManager = new LoginManager();
    //     loginManager.AddUser(username, password);
    //     LoginResponse? loginResponse = (loginManager.LoginRequest(username, password) as ObjectResult)?.Value as LoginResponse?;
    //     byte[] token = loginResponse!.Value.Token;
    //     ProductType? productType = ProductDatabase.Instance.AddProductType("Test", type);
    //     ProductManager productManager = new ProductManager();
    //     Customer customer = new Customer()
    //     Product product = new Product(productType!);
    //     
    //     ReserveRequest reserveRequest = new ReserveRequest(new Product(), token);
    //     ObjectResult? actual = productManager.ReserveRequest(reserveRequest) as ObjectResult;
    //     ReserveResponse? reserveResponse = actual?.Value as ReserveResponse?;
    //     new CentralContext().ProductMetadata.SingleOrDefault(meta => meta.Product.Type ==)
    //     
    //     Assert.IsInstanceOf<OkObjectResult>(actual);
    //     Assert.IsTrue(reserveResponse?.Success);
    // }
}