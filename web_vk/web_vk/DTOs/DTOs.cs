namespace web_vk.DTOs
{
    // ── Restaurant ──
    public class RestaurantDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public string? Description { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public string? OpenHours { get; set; }
        public double? Rating { get; set; }
        public string? ImagePath { get; set; }
        public double? Radius { get; set; }
        public List<AudioDto> Audios { get; set; } = new();
    }

    // ── Audio ──
    public class AudioDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string TextContent { get; set; } = "";
        public string LanguageCode { get; set; } = "vi-VN";
    }

    // ── Tour ──
    public class TourDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? TotalEstimatedTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TourStopDto> Stops { get; set; } = new();
    }

    public class TourStopDto
    {
        public int OrderIndex { get; set; }
        public RestaurantDto Restaurant { get; set; } = null!;
    }

    // ── Auth ──
    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? Email { get; set; }
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? Token { get; set; }
    }
}