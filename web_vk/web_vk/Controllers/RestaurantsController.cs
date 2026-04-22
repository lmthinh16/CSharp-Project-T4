using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_vk.DTOs;
using web_vk.Models;

namespace web_vk.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public RestaurantsController(AppDbContext context) => _context = context;

        // GET /api/restaurants
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Restaurants
                .Where(r => r.IsActive == true)
                .OrderBy(r => r.Priority ?? 0)
                .Include(r => r.Audios) // ← QUAN TRỌNG: phải include để App Sync nhận được Audio
                .Select(r => new RestaurantDto
                {
                    Id = r.Id,
                    Name = r.Name ?? "N/A",
                    Address = r.Address ?? "N/A",
                    Description = r.Description,
                    Lat = r.Lat,
                    Lng = r.Lng,
                    OpenHours = r.OpenHours,
                    Rating = r.Rating,
                    ImagePath = r.ImagePath,
                    Radius = r.Radius ?? 50,
                    IsActive = r.IsActive ?? false,
                    Audios = r.Audios.Select(a => new AudioDto
                    {
                        Id = a.Id,
                        Title = a.Title ?? "No Title",
                        TextContent = a.TextContent,
                        FilePath = a.FilePath ?? "",
                        LanguageCode = a.LanguageCode
                    }).ToList()
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET /api/restaurants/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var r = await _context.Restaurants
                .Include(r => r.Audios)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (r == null) return NotFound();

            var dto = new RestaurantDto
            {
                Id = r.Id,
                Name = r.Name ?? "N/A",
                Address = r.Address ?? "N/A",
                Description = r.Description,
                Lat = r.Lat,
                Lng = r.Lng,
                OpenHours = r.OpenHours,
                Rating = r.Rating,
                ImagePath = r.ImagePath,
                Radius = r.Radius ?? 50,
                IsActive = r.IsActive ?? false,
                Audios = r.Audios.Select(a => new AudioDto
                {
                    Id = a.Id,
                    Title = a.Title ?? "No Title",
                    TextContent = a.TextContent,
                    FilePath = a.FilePath ?? "",
                    LanguageCode = a.LanguageCode
                }).ToList()
            };

            return Ok(dto);
        }

        // GET /api/restaurants/nearby?lat=10.76&lng=106.70
        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearby(double lat, double lng)
        {
            var list = await _context.Restaurants
                // Sửa lỗi logic && giữa bool? và bool
                .Where(r => r.IsActive == true && r.Lat != null && r.Lng != null)
                .Include(r => r.Audios)
                .Select(r => new RestaurantDto
                {
                    Id = r.Id,
                    Name = r.Name ?? "N/A",
                    Address = r.Address ?? "N/A",
                    Description = r.Description,
                    Lat = r.Lat,
                    Lng = r.Lng,
                    OpenHours = r.OpenHours,
                    Rating = r.Rating,
                    ImagePath = r.ImagePath,
                    Radius = r.Radius ?? 50,
                    IsActive = r.IsActive ?? false,
                    Audios = r.Audios.Select(a => new AudioDto
                    {
                        Id = a.Id,
                        Title = a.Title ?? "No Title",
                        TextContent = a.TextContent,
                        FilePath = a.FilePath ?? "",
                        LanguageCode = a.LanguageCode
                    }).ToList()
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}