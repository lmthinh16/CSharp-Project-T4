namespace web_vk.Models
{
    public class Tour
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? TotalEstimatedTime { get; set; }  // phải là string, không phải int
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<TourDetail> TourDetails { get; set; } = new List<TourDetail>();
    }
}
