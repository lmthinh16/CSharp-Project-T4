namespace web_vk.Models
{
    public class Restaurant
    {
        public int Id { get; set; }

        public string? Name { get; set; } = null!;
        public string? Address { get; set; } = null!;

        public string? Description { get; set; }

        public double? Lat { get; set; }
        public double? Lng { get; set; }

        public string? OpenHours { get; set; }
        public double? Rating { get; set; }
        public string? ImagePath { get; set; }

        public string? AudioPath { get; set; }
        public double? Radius { get; set; } = 50; // Mặc định 50 mét
        public int? Priority { get; set; } = 0;
        public bool? IsActive { get; set; } = true;
        public virtual ICollection<Audio> Audios { get; set; } = new List<Audio>();
    }
}
