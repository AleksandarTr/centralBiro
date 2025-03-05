using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

[Table("customer")]
[PrimaryKey(nameof(Id))]
public class Customer(int id, string name, string address)
{
    public int Id { get; set; } = id;
    [StringLength(50)] public string Name { get; set; } = name;
    [StringLength(100)] public string Address { get; set; } = address;
}