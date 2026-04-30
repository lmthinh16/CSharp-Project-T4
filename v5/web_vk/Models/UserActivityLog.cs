// Models/UserActivityLog.cs
namespace web_vk.Models;

public class UserActivityLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public int RestaurantId { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? ActionType { get; set; }
    public int? DurationListened { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? LanguageCode { get; set; }

    // Navigation
    public Restaurant? Restaurant { get; set; }
}