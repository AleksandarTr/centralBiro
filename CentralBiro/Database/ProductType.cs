using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Database;

[Table("product_type")]
[PrimaryKey(nameof(Id))]
public class ProductType(string name, int id, int nextSerialNumber)
{
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
            Product? product = context.Products.SingleOrDefault(product => product.Type == Id && product.SerialNumber == serialNumber);
            if (product != null) return false;

            if(_reservedNumbers.Contains(serialNumber)) return false;
            if(_nextSerialNumber < serialNumber) return false;
            _reservedNumbers.Add(serialNumber);
            if(serialNumber == _nextSerialNumber) _nextSerialNumber++;
            return true;
        }
    }
}