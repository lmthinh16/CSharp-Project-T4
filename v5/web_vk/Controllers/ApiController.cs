using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_vk.DTOs;
using web_vk.Models;

namespace web_vk.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ApiController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ─────────────────────────────────────────────
    // 1. RESTAURANTS / POI
    // ─────────────────────────────────────────────

    /// <summary>
    /// App gọi lúc khởi động để tải toàn bộ POI (kèm audio)
    /// GET /api/restaurants
    /// </summary>
    [HttpGet("restaurants")]
    public async Task<IActionResult> GetRestaurants()
    {
        var list = await _db.Restaurants
            .Where(r => r.IsActive == true)
            .Include(r => r.Audios)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>
    /// Lấy POI trong bán kính (App dùng để tải nearby)
    /// GET /api/restaurants/nearby?lat=10.76&lng=106.70&radius=500
    /// </summary>
    [HttpGet("restaurants/nearby")]
    public async Task<IActionResult> GetNearby(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radius = 500)
    {
        var all = await _db.Restaurants
            .Where(r => r.IsActive == true)
            .Include(r => r.Audios)
            .ToListAsync();

        var nearby = all
            .Where(r => r.Lat.HasValue && r.Lng.HasValue)
            .Where(r => Haversine(lat, lng, r.Lat!.Value, r.Lng!.Value) <= radius)
            .OrderByDescending(r => r.Priority)
            .ToList();

        return Ok(nearby);
    }

    /// <summary>
    /// Lấy 1 POI chi tiết
    /// GET /api/restaurants/5
    /// </summary>
    [HttpGet("restaurants/{id:int}")]
    public async Task<IActionResult> GetRestaurant(int id)
    {
        var r = await _db.Restaurants
            .Include(r => r.Audios)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (r == null) return NotFound();
        return Ok(r);
    }

    /// <summary>
    /// App sync: lấy restaurants mới/cập nhật sau một thời điểm
    /// GET /api/sync/restaurants?since=2026-04-01T00:00:00
    /// </summary>
    [HttpGet("sync/restaurants")]
    public async Task<IActionResult> SyncRestaurants([FromQuery] DateTime? since)
    {
        var query = _db.Restaurants.Include(r => r.Audios).AsQueryable();
        // Nếu model của bạn có UpdatedAt thì filter theo đó
        // query = query.Where(r => r.UpdatedAt >= since);
        var list = await query.ToListAsync();
        return Ok(new { timestamp = DateTime.UtcNow, data = list });
    }

    // ─────────────────────────────────────────────
    // 2. TOURS
    // ─────────────────────────────────────────────

    /// <summary>
    /// GET /api/tours
    /// </summary>
    [HttpGet("tours")]
    public async Task<IActionResult> GetTours()
    {
        var tours = await _db.Tours
            .Include(t => t.TourDetails)
                .ThenInclude(td => td.Restaurant)
            .ToListAsync();
        return Ok(tours);
    }

    /// <summary>
    /// GET /api/tours/5
    /// </summary>
    [HttpGet("tours/{id:int}")]
    public async Task<IActionResult> GetTour(int id)
    {
        var tour = await _db.Tours
            .Include(t => t.TourDetails.OrderBy(td => td.OrderIndex))
                .ThenInclude(td => td.Restaurant)
                    .ThenInclude(r => r.Audios)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (tour == null) return NotFound();
        return Ok(tour);
    }

    // ─────────────────────────────────────────────
    // 3. ACTIVITY LOGS (App ghi lại hành vi)
    // ─────────────────────────────────────────────

    /// <summary>
    /// App gọi mỗi khi phát audio thành công
    /// POST /api/logs/activity
    /// Body: { restaurantId, lat, lng, actionType, durationListened, languageCode }
    /// </summary>
    [HttpPost("logs/activity")]
    public async Task<IActionResult> LogActivity([FromBody] ActivityLogDto dto)
    {
        if (dto.RestaurantId <= 0) return BadRequest("RestaurantId required");

        var log = new UserActivityLog
        {
            RestaurantId = dto.RestaurantId,
            Lat = dto.Lat,
            Lng = dto.Lng,
            ActionType = dto.ActionType ?? "played",
            DurationListened = dto.DurationListened,
            LanguageCode = dto.LanguageCode ?? "vi-VN",
            CreatedAt = DateTime.Now
        };

        _db.UserActivityLogs.Add(log);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ─────────────────────────────────────────────
    // 4. ANALYTICS (Dashboard CMS)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Top POI được nghe nhiều nhất
    /// GET /api/analytics/top-pois?top=10
    /// </summary>
    [HttpGet("analytics/top-pois")]
    public async Task<IActionResult> GetTopPois([FromQuery] int top = 10)
    {
        var result = await _db.UserActivityLogs
            .Where(l => l.ActionType == "played")
            .GroupBy(l => l.RestaurantId)
            .Select(g => new
            {
                RestaurantId = g.Key,
                PlayCount = g.Count(),
                AvgDuration = g.Average(l => (double?)(l.DurationListened ?? 0)) ?? 0
            })
            .OrderByDescending(x => x.PlayCount)
            .Take(top)
            .ToListAsync();

        // Join tên restaurant
        var restaurantIds = result.Select(r => r.RestaurantId).ToList();
        var restaurants = await _db.Restaurants
            .Where(r => restaurantIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();

        var combined = result.Select(r => new
        {
            r.RestaurantId,
            Name = restaurants.FirstOrDefault(x => x.Id == r.RestaurantId)?.Name ?? "Unknown",
            r.PlayCount,
            AvgDurationSeconds = Math.Round(r.AvgDuration, 1)
        });

        return Ok(combined);
    }

    /// <summary>
    /// Dữ liệu heatmap vị trí người dùng
    /// GET /api/analytics/heatmap?days=30
    /// </summary>
    [HttpGet("analytics/heatmap")]
    public async Task<IActionResult> GetHeatmap([FromQuery] int days = 30)
    {
        var since = DateTime.Now.AddDays(-days);
        var points = await _db.UserActivityLogs
            .Where(l => l.CreatedAt >= since)
            .Select(l => new { l.Lat, l.Lng, l.CreatedAt })
            .ToListAsync();
        return Ok(points);
    }

    /// <summary>
    /// Tổng quan dashboard
    /// GET /api/analytics/summary
    /// </summary>
    [HttpGet("analytics/summary")]
    public async Task<IActionResult> GetSummary()
    {
        var today = DateTime.Today;
        var thisMonth = new DateTime(today.Year, today.Month, 1);

        var summary = new
        {
            TotalPlays = await _db.UserActivityLogs.CountAsync(l => l.ActionType == "played"),
            PlaysToday = await _db.UserActivityLogs
                .CountAsync(l => l.ActionType == "played" && l.CreatedAt >= today),
            PlaysThisMonth = await _db.UserActivityLogs
                .CountAsync(l => l.ActionType == "played" && l.CreatedAt >= thisMonth),
            TotalPois = await _db.Restaurants.CountAsync(r => r.IsActive == true),
            TotalTours = await _db.Tours.CountAsync(),
        };

        return Ok(summary);
    }

    // ─────────────────────────────────────────────
    // 5. AUTH – App Login & Heartbeat
    // ─────────────────────────────────────────────

    /// <summary>
    /// App gọi khi user đăng nhập từ mobile
    /// POST /api/auth/login
    /// </summary>
    [HttpPost("auth/login")]
    public async Task<IActionResult> AppLogin([FromBody] AppLoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { success = false, message = "Thiếu thông tin đăng nhập." });

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == dto.Username);

        if (user == null || user.Password != dto.Password)
            return Ok(new { success = false, message = "Sai tài khoản hoặc mật khẩu." });

        if (user.IsLocked)
            return Ok(new { success = false, message = "Tài khoản đã bị khoá." });

        // ✅ Cập nhật LastActiveAt ngay khi login
        user.LastActiveAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success  = true,
            userId   = user.Id,
            username = user.Username,
            fullName = user.Username,
            role     = "user",
            message  = "Đăng nhập thành công."
        });
    }

    /// <summary>
    /// App ping định kỳ (mỗi 2 phút) để duy trì trạng thái online
    /// POST /api/heartbeat
    /// Body: { "userId": 5 }  ← 0 nếu guest
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatDto dto)
    {
        if (dto.UserId > 0)
        {
            var user = await _db.Users.FindAsync(dto.UserId);
            if (user != null)
            {
                user.LastActiveAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
        }
        return Ok(new { success = true, serverTime = DateTime.Now });
    }

    // ─────────────────────────────────────────────
    // HELPER: Haversine formula tính khoảng cách (mét)
    // ─────────────────────────────────────────────
    private static double Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}

// ─────────────────────────────────────────────
// DTO cho Activity Log
// ─────────────────────────────────────────────
public class AppLoginDto
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class HeartbeatDto
{
    public int UserId { get; set; }  // 0 = guest
}

public class ActivityLogDto
{
    public int RestaurantId { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? ActionType { get; set; }  // "played", "skipped", "entered_zone"
    public int? DurationListened { get; set; }
    public string? LanguageCode { get; set; }
}