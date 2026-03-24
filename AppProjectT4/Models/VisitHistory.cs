using SQLite;

namespace ProjectApp.Models
{
    [Table("visit_history")]
    public class VisitHistory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int RestaurantId { get; set; }

        // Use DateTimeOffset for timezone-safe storage; store UTC consistently.
        public DateTimeOffset VisitedAt { get; set; }
    }
}
