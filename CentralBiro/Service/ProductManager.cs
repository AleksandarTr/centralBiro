using CentralBiro.Contract;
using CentralBiro.Database;
using Microsoft.AspNetCore.Mvc;

namespace CentralBiro.Service;

[ApiController]
[Route("api/product")]
public class ProductManager : ControllerBase
{
    [HttpPost("reserve")]
    public IActionResult ReserveRequest([FromBody] ReserveRequest request)
    {
        LoginManager loginManager = new();
        
        if(request.Product == null || request.Token == null) 
            return BadRequest(new ReserveResponse(false, 0));
        if(!loginManager.Verify(request.Token)) 
            return Unauthorized(new ReserveResponse(false, 0));
        
        ProductType? productType = ProductDatabase.Instance.GetProductType(request.Product.Type);
        if(productType == null) return BadRequest(new ReserveResponse(false, 0));
        
        if (!productType.ReserveSerialNumber(request.Product.SerialNumber)) 
            request.Product.SerialNumber = productType.GetNextSerialNumber();
        int correction = request.Product.SerialNumber;

        //Product product = new Product(request.Product.Type, request.Product.SerialNumber);
        using CentralContext context = new();
        context.SaveChanges();
        
        return Ok(new ReserveResponse(true, correction));
    }
    
    
}