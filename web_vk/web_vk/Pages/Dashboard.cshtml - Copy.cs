using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using web_vk.Models; // Đảm bảo đúng namespace chứa AppDbContext

namespace web_vk.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        // Khai báo các biến để lưu số lượng
        public int TotalRestaurants { get; set; }
        public int TotalAudio { get; set; }
        public int TotalTours { get; set; }

        public async Task OnGetAsync()
        {
            // Đếm số lượng từ Database
            TotalRestaurants = await _context.Restaurants.CountAsync();
            TotalAudio = await _context.Audios.CountAsync();
            // TotalTours = await _context.Tours.CountAsync(); // Mở comment nếu bạn đã có bảng Tours
        }
    }
}