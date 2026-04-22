namespace web_vk.Models
{
    public class Audio
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; } = "OFFLINE_MODE";
        public string FilePath { get; set; } = "OFFLINE_MODE";
        public string TextContent { get; set; }
        public string VoiceName { get; set; } = "System Default";
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Thêm trường này để khớp với DB
        public string LanguageCode { get; set; } = "vi-VN";

        public int? RestaurantId { get; set; }
        public virtual Restaurant? Restaurant { get; set; }
        public bool IsGeneratedByTTS { get; set; } = true;
    }
}