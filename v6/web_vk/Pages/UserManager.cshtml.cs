using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using web_vk.Controllers;
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
            _cache   = cache;
        }

        public List<User> AllUsers      { get; set; } = new();
        public int        OnlineMembers { get; set; }
        public int        OnlineGuests  { get; set; }
        public int        TotalOnline   => OnlineMembers + OnlineGuests;

        [BindProperty(SupportsGet = true)] public string? SearchQuery  { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            var cutoff = DateTime.Now.AddMinutes(-5);

            // Tổng mobile users từ cache (guest + registered)
            _cache.TryGetValue(ApiController.MobileCountKey, out int total);
            OnlineMembers = await _context.Users
                .CountAsync(u => u.LastActiveAt != null && u.LastActiveAt >= cutoff);
            OnlineGuests = Math.Max(0, total - OnlineMembers);

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
                query = query.Where(u =>
                    u.Username.Contains(SearchQuery) ||
                    (u.Email != null && u.Email.Contains(SearchQuery)));

            if (StatusFilter == "online")
                query = query.Where(u => u.LastActiveAt != null && u.LastActiveAt >= cutoff);
            else if (StatusFilter == "offline")
                query = query.Where(u => u.LastActiveAt == null || u.LastActiveAt < cutoff);

            AllUsers = await query.OrderByDescending(u => u.LastActiveAt).ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null) { _context.Users.Remove(user); await _context.SaveChangesAsync(); }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleLockAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null) { user.IsLocked = !user.IsLocked; await _context.SaveChangesAsync(); }
            return RedirectToPage();
        }
    }
}