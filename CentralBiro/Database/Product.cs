using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

[Table("product")]
[PrimaryKey(nameof(Id))]
public class Product(int type, int serialNumber, Customer customer, DateTime reserveTime)
{
    public int Id { get; set; }
    public int Type { get; set; } = type;
    public int SerialNumber { get; set; } = serialNumber;
    public Customer Customer { get; set; } = customer;
    public DateTime ReserveTime { get; set; } = reserveTime;

    public Product(ProductType productType, Customer customer) :
        this(productType.Id, productType.GetNextSerialNumber(), customer, DateTime.Now) { }
    
    public Product() : this(0, 0, null, DateTime.Now) { }
}