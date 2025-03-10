using CentralBiro.Database;

namespace CentralBiro.Contract;

public struct ReserveRequest(Product product, byte[] token)
{
    public Product? Product { get; set; } = product;
    public byte[]? Token { get; set; } = token;
}