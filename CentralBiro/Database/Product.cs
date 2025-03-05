using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

[Table("product")]
[PrimaryKey(nameof(Type), nameof(SerialNumber))]
public class Product(int type, int serialNumber, Customer customer)
{
    public int Type { get; set; } = type;
    public int SerialNumber { get; set; } = serialNumber;
    public Customer Customer { get; set; } = customer;

    public Product(ProductType productType, Customer customer) :
        this(productType.Id, productType.GetNextSerialNumber(), customer) { }
}