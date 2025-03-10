using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

[Table("product_type")]
[PrimaryKey(nameof(Id))]
[Index(nameof(Name), IsUnique = true)]
public class ProductType(string name, int id, int nextSerialNumber)
{
    [StringLength(50)]
    public string Name { get; set; } = name;
    public int Id { get; set; } = id;
    [NotMapped]
    private readonly object _lock = new();
    [NotMapped]
    private int _nextSerialNumber = nextSerialNumber;
    [NotMapped]
    private readonly List<int> _reservedNumbers = new();

    public int GetNextSerialNumber()
    {
        //TODO: Add ability for serial numbers to not start from 1
        lock(_lock) 
        {
            return _nextSerialNumber++;
        }
    }

    public bool ReserveSerialNumber(int serialNumber)
    {
        lock (_lock)
        {
            using var context = new CentralContext();
            Product? product = context.Products.SingleOrDefault(prod => prod.Type == Id && prod.SerialNumber == serialNumber);
            if (product != null) return false;

            if(_reservedNumbers.Contains(serialNumber)) return false;
            if(_nextSerialNumber < serialNumber) return false;
            _reservedNumbers.Add(serialNumber);
            if(serialNumber == _nextSerialNumber) _nextSerialNumber++;
            return true;
        }
    }
    
    public ProductType() : this(null, 0, 0) {}
}