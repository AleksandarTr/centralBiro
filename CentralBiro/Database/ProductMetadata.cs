using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

[Table("product_metadata")]
[PrimaryKey(nameof(Product))]
public class ProductMetadata(Product product, User user)
{
    public Product Product { get; set; } = product;
    public User User { get; set; } = user;
    public DateTime ReserveTime { get; set; } = DateTime.UtcNow;
}