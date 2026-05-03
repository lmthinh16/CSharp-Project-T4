namespace web_vk.Models
{
    public class TourDetail
    {
        public int TourId { get; set; }
        public int RestaurantId { get; set; }
        public int OrderIndex { get; set; }

        public Tour? Tour { get; set; }
        public Restaurant? Restaurant { get; set; }
    }
}
