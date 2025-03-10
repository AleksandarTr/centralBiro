namespace CentralBiro.Contract;

public struct ReserveResponse(bool success, int correction)
{
    public bool Success { get; set; } = success;
    public int Correction { get; set; } = correction;
}