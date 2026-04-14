using SQLite;
using System.Text.Json;

namespace ProjectApp.Models
{
    [Table("tours")]
    public class Tour
    {
        // Keep string Id so you can use custom ids (e.g., "tour1").
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public double Rating { get; set; }

        // Persist the list of restaurant ids as JSON in the database.
        // sqlite-net does not natively store lists, so we serialize to a string column.
        public string RestaurantIdsJson { get; set; } = "[]";

        [Ignore]
        public List<int> RestaurantIds
        {
            get => JsonSerializer.Deserialize<List<int>>(RestaurantIdsJson) ?? new List<int>();
            set => RestaurantIdsJson = JsonSerializer.Serialize(value ?? new List<int>());
        }
    }
}
