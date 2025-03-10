using System.Linq;
using CentralBiro.Contract;
using CentralBiro.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Service;

[ApiController]
[Route("api/customer")]
public class CustomerManager : ControllerBase
{
    private Customer? GetById(int id)
    {
        using var context = new CentralContext();
        Customer? customer = context.Customers.Find(id);
        return customer;
    }

    private Customer? GetByName(string name)
    {
        using var context = new CentralContext();
        Customer? customer = context.Customers.SingleOrDefault(customer => customer.Name == name);
        return customer;
    }

    private Customer? GetByAddress(string address)
    {
        using var context = new CentralContext();
        Customer? customer = context.Customers.SingleOrDefault(customer => customer.Address == address);
        return customer;
    }

    public int AddCustomer(string name, string address)
    {
        var context = new CentralContext();
        Customer customer = new Customer(name, address);
        context.Customers.Add(customer);
        try
        {
            context.SaveChanges();
            return customer.Id;
        }
        catch (DbUpdateException)
        {
            return 0;
        }
    }

    /// <summary>
    /// <c>CreateRequest</c> is an instance of a <see cref="CrudRequest"/> requesting the addition
    /// of an instance of a customer.
    /// </summary>
    /// <param name="request">This <see cref="CrudRequest"/> takes two string arguments,
    /// the first being the name and the second being the address of the customer.</param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult CreateRequest([FromBody] CrudRequest request)
    {
        if (!new LoginManager().Verify(request.Token))
            return Unauthorized(new CrudResponse());
        
        if(request.StringArgs.Length < 2) return BadRequest(new CrudResponse());
        
        int id = AddCustomer(request.StringArgs[0], request.StringArgs[1]);
        if(id == 0) return BadRequest(new CrudResponse());
        return Ok(new CrudResponse(null, id, []));
    }

    /// <summary>
    /// <c>ReadRequest</c> is an instance of a <see cref="CrudRequest"/> requesting an instance of a customer.
    /// </summary>
    /// <param name="request">This <see cref="CrudRequest"/> has the first int argument which determines
    /// the search parameter as follows: <br/>
    /// If 1: it takes another int argument signifying the id<br/>
    /// If 2: it takes one string argument signifying the name<br/>
    /// If 3: it takes one string argument signifying the address</param>
    /// <returns></returns>
    [HttpGet]
    public IActionResult ReadRequest([FromBody] CrudRequest request)
    {
        if (!new LoginManager().Verify(request.Token))
            return Unauthorized(new CrudResponse());

        if (request.IntArgs.Length < 1) return BadRequest(new CrudResponse());

        Customer? result;
        switch (request.IntArgs[0])
        {
            case 1: //Id
                if (request.IntArgs.Length < 2) return BadRequest(new CrudResponse());
                result = GetById(request.IntArgs[1]);
                break;
            case 2: //Name
                if (request.StringArgs.Length < 1) return BadRequest(new CrudResponse());
                result = GetByName(request.StringArgs[0]);
                break;
            case 3: //Address
                if (request.StringArgs.Length < 1) return BadRequest(new CrudResponse());
                result = GetByAddress(request.StringArgs[0]);
                break;
            default:
                return BadRequest(new CrudResponse());
        }

        if (result is null) return NotFound(new CrudResponse());
        return Ok(new CrudResponse(result, 1, []));
    }

    /// <summary>
    /// <c>DeleteRequest</c> is an instance of a <see cref="CrudRequest"/> requesting the deletion
    /// of an instance of a customer.
    /// </summary>
    /// <param name="request">This <see cref="CrudRequest"/> takes one int argument signifying
    /// the id of the customer.</param>
    /// <remarks>The deletion will fail if the customer is associated with any product.</remarks>
    /// <returns></returns>
    [HttpDelete]
    public IActionResult DeleteRequest([FromBody] CrudRequest request)
    {
        if (!new LoginManager().Verify(request.Token))
            return Unauthorized(new CrudResponse());
        
        if (request.IntArgs.Length < 1) return BadRequest(new CrudResponse());
        Customer? customer = GetById(request.IntArgs[0]);
        if(customer is null) return NotFound(new CrudResponse());
        
        using var context = new CentralContext();
        if(context.Products.Count(prod => prod.Customer.Id == customer.Id) > 0)
            return Conflict(new CrudResponse());
        
        context.Customers.Remove(customer);
        try
        {
            context.SaveChanges();
            return Ok(new CrudResponse(null, 1, []));
        }
        catch (DbUpdateException)
        {
            return Conflict(new CrudResponse());
        }
    }

    /// <summary>
    /// <c>UpdateRequest</c> is an instance of a <see cref="CrudRequest"/> requesting the updating of
    /// an instance of a customer
    /// </summary>
    /// <param name="request">This <see cref="CrudRequest"/> takes one int argument signifying the
    /// id of the customer and two string arguments, signifying firstly their name and
    /// secondly their address.</param>
    /// <returns></returns>
    [HttpPut]
    public IActionResult UpdateRequest([FromBody] CrudRequest request)
    {
        if (!new LoginManager().Verify(request.Token))
            return Unauthorized(new CrudResponse());
        
        if (request.IntArgs.Length < 1) return BadRequest(new CrudResponse());
        if (request.StringArgs.Length < 2) return BadRequest(new CrudResponse());
        
        Customer? customer = GetById(request.IntArgs[0]);
        if(customer is null) return NotFound(new CrudResponse());
        
        using var context = new CentralContext();
        customer.Name = request.StringArgs[0];
        customer.Address = request.StringArgs[1];
        context.Customers.Update(customer);

        try
        {
            context.SaveChanges();
            return Ok(new CrudResponse(null, 1, []));
        }
        catch (DbUpdateException)
        {
            return Conflict(new CrudResponse());
        }
    }
}