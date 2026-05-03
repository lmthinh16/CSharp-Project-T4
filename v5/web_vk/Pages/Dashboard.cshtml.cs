using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using web_vk.Middleware;
using web_vk.Models;

namespace web_vk.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        public DashboardModel(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public int TotalRestaurants { get; set; }
        public int TotalAudio { get; set; }
        public int TotalTours { get; set; }

        // ✅ FIX: Số người đang online (đăng nhập + ẩn danh)
        public int OnlineLoggedIn { get; set; }
        public int OnlineAnonymous { get; set; }
        public int OnlineTotal => OnlineLoggedIn + OnlineAnonymous;

        public async Task OnGetAsync()
        {
            TotalRestaurants = await _context.Restaurants.CountAsync();
            TotalAudio = await _context.Audios.CountAsync();
            TotalTours = await _context.Tours.CountAsync();

            // ── Người dùng đã đăng nhập: LastActiveAt trong vòng 5 phút ──
            var cutoff = DateTime.Now.AddMinutes(-5);
            OnlineLoggedIn = await _context.Users
                .CountAsync(u => u.LastActiveAt != null && u.LastActiveAt >= cutoff);

            // ── Khách ẩn danh: đếm từ counter riêng trong MemoryCache ──
            // KHÔNG dùng reflection vào _entries/_coherentState vì .NET 9 đã bỏ field đó.
            // OnlineTrackerMiddleware lưu key "anon_count" là số lượng visitor active.
            if (_cache.TryGetValue(OnlineTrackerMiddleware.AnonCountKey, out int anonCount))
            {
                OnlineAnonymous = anonCount;
            }
        }
    }
}