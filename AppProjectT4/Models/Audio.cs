using SQLite;

namespace ProjectApp.Models
{
    [Table("audios")]
    public class Audio
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int RestaurantId { get; set; }
        
        public string VoiceName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty;
        public bool IsGeneratedByTTS { get; set; } = true;
        
        // "vi-VN", "en-US", "zh-CN"
        [Indexed]
        public string LanguageCode { get; set; } = string.Empty;
        
        public int Duration { get; set; } = 0;
        public int CooldownMinutes { get; set; } = 10;
    }
}
