using CentralBiro.Database;
using Microsoft.AspNetCore.Mvc;

namespace CentralBiro.Service;

public class ProductManager
{
    public static ProductManager Instance { get; } = new ProductManager();
    private ProductManager() {}
    
    
}

public struct ReserveRequest(Product product, byte[] token)
{
    public Product Product { get; set; } = product;
    public byte[] Token { get; set; } = token;
}

public struct ReserveResponse(bool success, int correction)
{
    public bool Success { get; set; } = success;
    public int Correction { get; set; } = correction;
}

[ApiController]
[Route("api/product")]
public class ProductController : ControllerBase
{
    [HttpPost("reserve")]
    public IActionResult ReserveRequest([FromBody] ReserveRequest request)
    {
        if(request.Product == null || request.Token == null) 
            return BadRequest(new ReserveResponse(false, 0));
        if(!LoginManager.Instance.Verify(request.Token)) 
            return Unauthorized(new ReserveResponse(false, 0));
        
        ProductType? productType = ProductDatabase.Instance.GetProductType(request.Product.Type);
        if(productType == null) return BadRequest(new ReserveResponse(false, 0));
        
        //TODO: Check if customer exists

        int correction = 0;
        if (!productType.ReserveSerialNumber(request.Product.SerialNumber))
        {
            correction = productType.GetNextSerialNumber();
            request.Product.SerialNumber = correction;
        }

        ProductMetadata metadata = new ProductMetadata(request.Product, LoginManager.Instance.GetUser(request.Token)!);
        using CentralContext context = new();
        context.Products.Add(request.Product);
        context.ProductMetadata.Add(metadata);
        context.SaveChanges();
        
        return Ok(new ReserveResponse(true, correction));
    }
}