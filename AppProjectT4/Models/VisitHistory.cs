using SQLite;

namespace AppProjectT4.Models
{
    [Table("visit_history")]
    public class VisitHistory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int RestaurantId { get; set; }
        public DateTime VisitedAt { get; set; }
    }
}