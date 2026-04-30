using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using web_vk.Models;

namespace web_vk.Pages
{
    public class TranslationsModel : PageModel
    {
        private readonly AppDbContext _context;
        public TranslationsModel(AppDbContext context) => _context = context;

        // Danh sách định nghĩa 5 ngôn ngữ mục tiêu
        public List<(string Code, string Name, string Flag)> AvailableLanguages = new()
        {
            ("vi-VN", "Tiếng Việt", "🇻🇳"),
            ("en-US", "Tiếng Anh", "🇺🇸"),
            ("zh-CN", "Tiếng Trung", "🇨🇳"),
            ("ja-JP", "Tiếng Nhật", "🇯🇵"),
            ("ko-KR", "Tiếng Hàn", "🇰🇷")
        };

        public List<RestaurantTranslationDTO> RestaurantGroups { get; set; } = new();
        public List<Audio> FullScripts { get; set; } = new();
        public async Task OnGetAsync()
        {
            RestaurantGroups = await _context.Restaurants
                .Include(r => r.Audios)
                .AsNoTracking()
                .Select(r => new RestaurantTranslationDTO
                {
                    Id = r.Id,
                    Name = r.Name,
                    Address = r.Address,
                    ExistingLanguages = r.Audios.Select(a => a.LanguageCode).ToList()
                })
                .ToListAsync();

            // Lấy thêm danh sách chi tiết kịch bản để xử lý Modal
            FullScripts = await _context.Audios.AsNoTracking().ToListAsync();
        }

        // Thêm Handler Xóa kịch bản ngay tại trang này
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var audio = await _context.Audios.FindAsync(id);
            if (audio != null)
            {
                _context.Audios.Remove(audio);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa kịch bản!";
            }
            return RedirectToPage();
        }
    }

    // Lớp vận chuyển dữ liệu (DTO) để hiển thị trên giao diện
    public class RestaurantTranslationDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public List<string> ExistingLanguages { get; set; }

        // Tính % hoàn thành dựa trên 5 ngôn ngữ
        public int Progress => (ExistingLanguages.Distinct().Count() * 100) / 5;

        // Màu sắc động cho thanh tiến độ
        public string StatusColor => Progress == 100 ? "#22c55e" : (Progress >= 60 ? "#c9a84c" : "#ef4444");
    }

}