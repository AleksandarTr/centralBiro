using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

[Table("customer")]
[PrimaryKey(nameof(Id))]
[Index(nameof(Name), nameof(Address), IsUnique = true)]
public class Customer(int id, string name, string address)
{
    public int Id { get; set; } = id;
    [StringLength(50)] public string Name { get; set; } = name;
    [StringLength(100)] public string Address { get; set; } = address;
    
    public Customer() : this(0, null, null) { }

    public Customer(string name, string address) : this(0, name, address)
    {
        int id;
        try
        {
            id = new CentralContext().Customers.Max(customer => customer.Id) + 1;
        }
        catch (DbUpdateException)
        {
            id = 1;
        }

        Id = id;
    }
}