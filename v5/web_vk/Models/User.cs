public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public bool IsLocked { get; set; } = false;
    public string? Email { get; set; }           // ← thêm
    public DateTime CreatedAt { get; set; } = DateTime.Now; // ← thêm
    public DateTime? LastActiveAt { get; set; }
}