namespace CentralBiro.Contract;

/// <summary>
/// <c>LoginResponse</c> struct is used to store data pertaining to the server's response to a login attempt 
/// </summary>
/// <param name="success">Indicates whether the login has been successful</param>
/// <param name="token">If <see cref="Success"/> is true, contains a valid non-expired token associated with the user
/// otherwise, it may contain any data</param>
public struct LoginResponse(bool success, byte[] token)
{
    ///<value>Indicates whether the login has been successful</value>
    public bool Success { get; set; } = success;
    ///<value>If <see cref="Success"/> is true, contains a valid non-expired token associated with the user
    /// otherwise, it may contain any data</value>
    public byte[] Token { get; set; } = token;
}