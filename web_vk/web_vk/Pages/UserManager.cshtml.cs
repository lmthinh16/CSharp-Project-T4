using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using web_vk.Middleware;
using web_vk.Models;

namespace web_vk.Pages
{
    public class UserManagerModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        public UserManagerModel(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // ── Dữ liệu hiển thị ──
        public List<User> AllUsers { get; set; } = new();
        public int OnlineMembers { get; set; }
        public int OnlineAnonymous { get; set; }
        public int TotalOnline => OnlineMembers + OnlineAnonymous;

        // ── Filter / Search ──
        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; } // "all" | "online" | "offline"

        public async Task OnGetAsync()
        {
            var fiveMinutesAgo = DateTime.Now.AddMinutes(-5);

            // Đếm member đang online
            OnlineMembers = await _context.Users
                .CountAsync(u => u.LastActiveAt != null && u.LastActiveAt >= fiveMinutesAgo);

            // Đếm khách ẩn danh từ MemoryCache
            // IMemoryCache không có GetAllKeys() trực tiếp nên dùng IMemoryCacheManager
            // Cách đơn giản: dùng static counter (xem OnlineCounterService bên dưới)
            // Ở đây dùng reflection trick để lấy keys từ MemoryCache
            OnlineAnonymous = GetAnonymousCount();

            // Lấy danh sách user
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
                query = query.Where(u =>
                    u.Username.Contains(SearchQuery) ||
                    (u.Email != null && u.Email.Contains(SearchQuery)));

            if (StatusFilter == "online")
                query = query.Where(u => u.LastActiveAt != null && u.LastActiveAt >= fiveMinutesAgo);
            else if (StatusFilter == "offline")
                query = query.Where(u => u.LastActiveAt == null || u.LastActiveAt < fiveMinutesAgo);

            AllUsers = await query.OrderByDescending(u => u.LastActiveAt).ToListAsync();
        }

        // Xóa tài khoản
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        // Khoá / Mở khoá tài khoản
        public async Task<IActionResult> OnPostToggleLockAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsLocked = !user.IsLocked;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        // Đếm anonymous visitors từ MemoryCache
        private int GetAnonymousCount()
        {
            try
            {
                // Lấy internal entries từ MemoryCache qua reflection
                var field = typeof(MemoryCache).GetField("_coherentState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var coherentState = field?.GetValue(_cache);
                var entriesField = coherentState?.GetType().GetField("_entries",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var entries = entriesField?.GetValue(coherentState) as System.Collections.IDictionary;

                if (entries == null) return 0;

                int count = 0;
                foreach (var key in entries.Keys)
                    if (key.ToString()!.StartsWith(OnlineTrackerMiddleware.AnonPrefix))
                        count++;
                return count;
            }
            catch
            {
                return 0;
            }
        }
    }
}
