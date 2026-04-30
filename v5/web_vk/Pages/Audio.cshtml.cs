using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using web_vk.Models;

namespace web_vk.Pages
{
    public class AudioModel : PageModel
    {
        private readonly AppDbContext _context;

        public AudioModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Audio> AudioList { get; set; } = new();
        public List<Restaurant> Restaurants { get; set; } = new();

        public async Task OnGetAsync(int? id, string mode)
        {
            AudioList = await _context.Audios
                .Include(a => a.Restaurant)
                .AsNoTracking()
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();

            Restaurants = await _context.Restaurants
                .AsNoTracking()
                .ToListAsync();

            // --- ĐÃ SỬA: Kiểm tra nếu từ trang Translations bấm "Sửa" sang ---
            if (id.HasValue && mode == "edit")
            {
                var editData = await _context.Audios.FirstOrDefaultAsync(a => a.Id == id);
                if (editData != null)
                {
                    // Đổ dữ liệu vào ViewData để file .cshtml hiển thị lên Form
                    ViewData["EditId"] = editData.Id;
                    ViewData["EditTitle"] = editData.Title;
                    ViewData["EditText"] = editData.TextContent;
                    ViewData["EditLang"] = editData.LanguageCode;
                    ViewData["EditResId"] = editData.RestaurantId;
                    ViewData["IsEditMode"] = true;
                }
            }
        }

        /// <summary>
        /// Xử lý LƯU hoặc CẬP NHẬT kịch bản
        /// </summary>
        public async Task<IActionResult> OnPostGenerateAsync(int? id, string title, string text, int restaurantId, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                TempData["Error"] = "Vui lòng nhập nội dung văn bản!";
                return RedirectToPage();
            }

            try
            {
                int? rId = restaurantId > 0 ? restaurantId : null;

                // --- ĐÃ SỬA: Nếu có ID truyền vào thì thực hiện CẬP NHẬT ---
                if (id.HasValue && id > 0)
                {
                    var existing = await _context.Audios.FindAsync(id);
                    if (existing != null)
                    {
                        existing.Title = string.IsNullOrWhiteSpace(title) ? "Kịch bản thuyết minh" : title;
                        existing.TextContent = text;
                        existing.LanguageCode = languageCode;
                        existing.RestaurantId = rId;
                        existing.UploadedAt = DateTime.Now;

                        _context.Audios.Update(existing);
                        TempData["Success"] = "Đã cập nhật kịch bản thành công!";
                    }
                }
                else
                {
                    // --- NGƯỢC LẠI: THÊM MỚI ---
                    var newAudio = new Audio
                    {
                        Title = string.IsNullOrWhiteSpace(title) ? "Kịch bản thuyết minh" : title,
                        TextContent = text,
                        LanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "vi-VN" : languageCode,
                        VoiceName = "System Default",
                        UploadedAt = DateTime.Now,
                        RestaurantId = rId,
                        IsGeneratedByTTS = true,
                        FileName = "OFFLINE_SCRIPT",
                        FilePath = "OFFLINE_MODE"
                    };
                    _context.Audios.Add(newAudio);
                    TempData["Success"] = $"Đã lưu kịch bản mới ({languageCode}) thành công!";
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            // Sau khi lưu hoặc sửa xong, quay về trang Tiến độ đa ngữ cho dễ quản lý
            return RedirectToPage("/Translations");
        }

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

        // Bạn có thể giữ hoặc xóa OnPostUpdateAsync cũ vì bây giờ OnPostGenerateAsync đã làm cả 2 việc
        public async Task<IActionResult> OnPostUpdateAsync(int id, string title, string text, string languageCode)
        {
            // Code cũ... (OnPostGenerateAsync bên trên đã thay thế cái này rồi)
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAssignAsync(int audioId, int restaurantId)
        {
            var audio = await _context.Audios.FindAsync(audioId);
            if (audio != null)
            {
                audio.RestaurantId = restaurantId > 0 ? restaurantId : null;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật liên kết địa điểm!";
            }
            return RedirectToPage();
        }
    }
}