using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_vk.DTOs;
using web_vk.Models;

namespace web_vk.Controllers
{
    // ════════════════════════════════════════
    //  TOURS CONTROLLER
    // ════════════════════════════════════════
    [ApiController]
    [Route("api/[controller]")]
    public class ToursController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ToursController(AppDbContext context) => _context = context;

        // GET /api/tours
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tours = await _context.Tours
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TourDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    TotalEstimatedTime = t.TotalEstimatedTime,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(tours);
        }

        // GET /api/tours/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.TourDetails)
                    .ThenInclude(td => td.Restaurant)
                        .ThenInclude(r => r!.Audios)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null) return NotFound();

            var dto = new TourDto
            {
                Id = tour.Id,
                Title = tour.Title,
                Description = tour.Description,
                TotalEstimatedTime = tour.TotalEstimatedTime,
                CreatedAt = tour.CreatedAt,
                Stops = tour.TourDetails
                    .OrderBy(td => td.OrderIndex)
                    .Select(td => new TourStopDto
                    {
                        OrderIndex = td.OrderIndex,
                        Restaurant = new RestaurantDto
                        {
                            Id = td.Restaurant!.Id,
                            Name = td.Restaurant.Name,
                            Address = td.Restaurant.Address,
                            Description = td.Restaurant.Description,
                            Lat = td.Restaurant.Lat,
                            Lng = td.Restaurant.Lng,
                            OpenHours = td.Restaurant.OpenHours,
                            Rating = td.Restaurant.Rating,
                            ImagePath = td.Restaurant.ImagePath,
                            Radius = td.Restaurant.Radius,
                            Audios = td.Restaurant.Audios.Select(a => new AudioDto
                            {
                                Id = a.Id,
                                Title = a.Title,
                                TextContent = a.TextContent,
                                FilePath = a.FilePath ?? "",
                                LanguageCode = a.LanguageCode
                            }).ToList()
                        }
                    }).ToList()
            };

            return Ok(dto);
        }
    }

    // ════════════════════════════════════════
    //  AUTH CONTROLLER
    // ════════════════════════════════════════
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AuthController(AppDbContext context) => _context = context;

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == req.Username && u.Password == req.Password);

            if (user == null)
                return Ok(new AuthResponse { Success = false, Message = "Sai tài khoản hoặc mật khẩu" });

            if (user.IsLocked)
                return Ok(new AuthResponse { Success = false, Message = "Tài khoản đã bị khóa" });

            // Cập nhật LastActiveAt
            user.LastActiveAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Success = true,
                UserId = user.Id,
                Username = user.Username,
                Token = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes($"{user.Id}:{user.Username}:{DateTime.Now.Ticks}"))
            });
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return Ok(new AuthResponse { Success = false, Message = "Vui lòng nhập đầy đủ thông tin" });

            var exists = await _context.Users.AnyAsync(u => u.Username == req.Username);
            if (exists)
                return Ok(new AuthResponse { Success = false, Message = "Tên đăng nhập đã tồn tại" });

            var newUser = new User
            {
                Username = req.Username,
                Password = req.Password,
                Email = req.Email,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Success = true,
                UserId = newUser.Id,
                Username = newUser.Username,
                Message = "Đăng ký thành công"
            });
        }

        // POST /api/auth/guest — tạo phiên khách không cần tài khoản
        [HttpPost("guest")]
        public IActionResult Guest()
        {
            var guestId = Guid.NewGuid().ToString();
            return Ok(new AuthResponse
            {
                Success = true,
                Username = "Khách",
                Token = $"guest_{guestId}"
            });
        }
    }
}
