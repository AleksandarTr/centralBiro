namespace CentralBiro.Service;

public struct GenericStatus(int value = 0) : IStatus
{
    public int Value { get; set; } = value;
}