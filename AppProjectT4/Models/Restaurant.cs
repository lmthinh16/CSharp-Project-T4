using SQLite;

namespace ProjectApp.Models
{
    [Table("restaurants")]
    public class Restaurant
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string AudioPath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string OpenHours { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string ImagePath { get; set; } = string.Empty;

        // Geofence fields
        public double Radius { get; set; } = 50.0;
        public int Priority { get; set; } = 1;
        
        public bool IsActive { get; set; } = true;
    }
}
