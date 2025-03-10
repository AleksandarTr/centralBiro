using CentralBiro.Database;
using CentralBiro.Service;

namespace CentralBiro.Contract;

public struct CrudRequest(int[] intArgs, string[] stringArgs,
    IStatus status, byte[] token)
{
    public int[] IntArgs { get; set; } = intArgs;
    public string[] StringArgs { get; set; } = stringArgs;
    public IStatus Status { get; set; } = status;
    public byte[] Token { get; set; } = token;
    
    public CrudRequest(): this([], [], new GenericStatus(), []) {}
}