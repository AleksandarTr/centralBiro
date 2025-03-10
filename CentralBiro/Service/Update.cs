namespace CentralBiro.Service;

public struct Update(IStatus status, string comment, int userId)
{
    public IStatus Status { get; set; } = status;
    public string Comment { get; set; } = comment;
    public int UserId { get; set; } = userId;
    
    public Update() : this(new GenericStatus(), null, 0) {}
}