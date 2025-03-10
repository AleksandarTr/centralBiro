using System.Linq;

namespace CentralBiro.Service;

public class ComplaintStatus(int value = 0) : IStatus
{
    public const int Placed = 0;
    public const int Resolved = 1;
    
    private int[] _validValues = [ Placed, Resolved];

    private int _value = value;
    public int Value
    {
        get => _value; 
        set => _value = _validValues.Contains(value) ? value : _value;
    }
}