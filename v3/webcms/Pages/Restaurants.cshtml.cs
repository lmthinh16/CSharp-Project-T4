using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using web_vk.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace web_vk.Pages
{
    public class RestaurantsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RestaurantsModel(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public List<Restaurant> list = new();

        public async Task OnGetAsync()
        {
            var rawData = await _context.Restaurants.AsNoTracking().ToListAsync();
            list = rawData
                .Where(r => r.Name != null && r.Address != null)
                .Select(r => {
                    r.Lat = FixDisplayCoord(r.Lat, "lat");
                    r.Lng = FixDisplayCoord(r.Lng, "lng");
                    return r;
                }).ToList();
        }

        public async Task<IActionResult> OnPostCreateAsync(
            int? id,
            string name,
            string address,
            string description,
            string? lat, // Chuyển sang string để xử lý dấu phẩy/chấm thủ công
            string? lng, // Chuyển sang string để xử lý dấu phẩy/chấm thủ công
            double? radius,
            string? openHours,
            double? rating,
            IFormFile image)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
            {
                TempData["Error"] = "Vui lòng nhập tên và địa chỉ!";
                return RedirectToPage();
            }

            try
            {
                // Xử lý nắn tọa độ từ chuỗi nhập vào để tránh biến thành số E+16
                double finalLat = ParseSafeCoord(lat, "lat");
                double finalLng = ParseSafeCoord(lng, "lng");

                if (finalLat == 0 || finalLng == 0)
                {
                    TempData["Error"] = "Tọa độ không hợp lệ!";
                    return RedirectToPage();
                }

                Restaurant r;
                bool isUpdate = false;

                if (id.HasValue && id > 0)
                {
                    r = await _context.Restaurants.FindAsync(id);
                    if (r == null) return NotFound();
                    isUpdate = true;
                }
                else
                {
                    r = new Restaurant();
                }

                r.Name = name;
                r.Address = address;
                r.Description = description;
                r.Lat = finalLat;
                r.Lng = finalLng;
                r.Radius = radius ?? 50;
                r.OpenHours = openHours;
                r.Rating = rating;

                if (image != null && image.Length > 0)
                {
                    var folder = Path.Combine(_env.WebRootPath, "uploads", "images");
                    Directory.CreateDirectory(folder);
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                    var fullPath = Path.Combine(folder, fileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    r.ImagePath = "/uploads/images/" + fileName;
                }

                if (isUpdate) _context.Restaurants.Update(r);
                else _context.Restaurants.Add(r);

                await _context.SaveChangesAsync();
                TempData["Success"] = isUpdate ? "Cập nhật thành công!" : "Thêm mới thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var r = await _context.Restaurants.FindAsync(id);
            if (r != null)
            {
                if (!string.IsNullOrEmpty(r.ImagePath))
                {
                    var fullPath = Path.Combine(_env.WebRootPath, r.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                }
                _context.Restaurants.Remove(r);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa địa điểm!";
            }
            return RedirectToPage();
        }

        // --- CÁC HÀM HỖ TRỢ XỬ LÝ TỌA ĐỘ RÁC ---

        private double ParseSafeCoord(string val, string type)
        {
            if (string.IsNullOrEmpty(val)) return 0;
            // Thay dấu phẩy thành dấu chấm và loại bỏ các ký tự lạ
            string s = val.Replace(",", ".").Trim();
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                // Nếu số quá lớn (> 1000), tiến hành cắt chuỗi để lấy đúng tọa độ VN
                if (Math.Abs(result) > 1000)
                {
                    string clean = s.Replace(".", "");
                    if (type == "lat") return double.Parse(clean.Substring(0, 2) + "." + clean.Substring(2, 6), CultureInfo.InvariantCulture);
                    else return double.Parse(clean.Substring(0, 3) + "." + clean.Substring(3, 6), CultureInfo.InvariantCulture);
                }
                return result;
            }
            return 0;
        }

        private double FixDisplayCoord(double? val, string type)
        {
            if (!val.HasValue || val == 0) return 0;
            double v = val.Value;
            // Nếu phát hiện số lỗi từ DB (quá lớn hoặc quá nhỏ do số mũ âm)
            if (Math.Abs(v) > 1000 || Math.Abs(v) < 1)
            {
                string s = v.ToString("G17").Replace(",", "").Replace(".", "").Replace("-", "");
                try
                {
                    if (type == "lat") return double.Parse(s.Substring(0, 2) + "." + s.Substring(2, 6), CultureInfo.InvariantCulture);
                    else return double.Parse(s.Substring(0, 3) + "." + s.Substring(3, 6), CultureInfo.InvariantCulture);
                }
                catch { return 0; }
            }
            return v;
        }
    }
}