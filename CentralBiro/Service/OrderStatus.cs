using System.Linq;

namespace CentralBiro.Service;

public struct OrderStatus(int value = 0) : IStatus
{
    public const int Placed = 0;
    public const int Processing = 1;
    public const int Delivered = 2;
    public const int Cancelled = 3;
    
    private int[] _validValues = [ Placed, Processing, Delivered, Cancelled ];

    private int _value = value;
    public int Value
    {
        get => _value; 
        set => _value = _validValues.Contains(value) ? value : _value;
    }
}