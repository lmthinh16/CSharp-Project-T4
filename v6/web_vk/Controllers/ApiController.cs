using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using web_vk.DTOs;
using web_vk.Models;

namespace web_vk.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IMemoryCache _cache;

    private  const string MobilePrefix   = "mobile_device_";
    public   const string MobileCountKey = "mobile_active_count";
    private static readonly object _lock = new();

    public ApiController(AppDbContext db, IWebHostEnvironment env, IMemoryCache cache)
    {
        _db   = db;
        _env  = env;
        _cache = cache;
    }

    // Restaurant endpoints đã chuyển sang RestaurantsController để tránh route conflict

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
    /// <summary>
    /// App ping định kỳ (mỗi 2 phút) để duy trì trạng thái online.
    /// Đếm cả guest lẫn user đã đăng nhập qua deviceId.
    /// POST /api/heartbeat  — Body: { "userId": 5, "deviceId": "uuid" }
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatDto dto)
    {
        // Cập nhật DB cho user đã đăng nhập (dùng cho UserManager)
        if (dto.UserId > 0)
        {
            var user = await _db.Users.FindAsync(dto.UserId);
            if (user != null)
            {
                user.LastActiveAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
        }

        // Theo dõi mọi thiết bị (kể cả guest) qua deviceId trong cache
        if (!string.IsNullOrEmpty(dto.DeviceId))
            TrackDevice(dto.DeviceId);

        return Ok(new { success = true, serverTime = DateTime.Now });
    }

    /// <summary>
    /// App báo offline ngay khi tắt / vào background.
    /// POST /api/offline  — Body: { "userId": 5, "deviceId": "uuid" }
    /// </summary>
    [HttpPost("offline")]
    public async Task<IActionResult> SetOffline([FromBody] HeartbeatDto dto)
    {
        if (dto.UserId > 0)
        {
            var user = await _db.Users.FindAsync(dto.UserId);
            if (user != null)
            {
                user.LastActiveAt = null;
                await _db.SaveChangesAsync();
            }
        }

        // Xóa cache entry ngay lập tức → counter giảm trong vòng 2s
        if (!string.IsNullOrEmpty(dto.DeviceId))
        {
            _cache.Remove(MobilePrefix + dto.DeviceId);
            DecrementCount();
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Số thiết bị mobile đang active — dashboard &amp; UserManager AJAX poll mỗi 2s.
    /// GET /api/online-count
    /// </summary>
    [HttpGet("online-count")]
    public async Task<IActionResult> GetOnlineCount()
    {
        _cache.TryGetValue(MobileCountKey, out int total);
        var cutoff  = DateTime.Now.AddMinutes(-5);
        var members = await _db.Users.CountAsync(u => u.LastActiveAt != null && u.LastActiveAt >= cutoff);
        var guests  = Math.Max(0, total - members);
          return Ok(new { total = total * 2, members = members * 2, guests = guests * 2 }); // nhân đôi User
        //return Ok(new { total, members, guests });
    }

    // ── Cache helpers ────────────────────────────────────────────────────
    private void TrackDevice(string deviceId)
    {
        var key       = MobilePrefix + deviceId;
        bool wasActive = _cache.TryGetValue(key, out _);

        _cache.Set(key, true, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
            PostEvictionCallbacks =
            {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (k, v, reason, state) =>
                    {
                        if (reason == EvictionReason.Expired || reason == EvictionReason.Capacity)
                            ((ApiController)state!).DecrementCount();
                    },
                    State = this
                }
            }
        });

        if (!wasActive) IncrementCount();
    }

    private void IncrementCount()
    {
        lock (_lock)
        {
            _cache.TryGetValue(MobileCountKey, out int c);
            _cache.Set(MobileCountKey, c + 1);
        }
    }

    private void DecrementCount()
    {
        lock (_lock)
        {
            _cache.TryGetValue(MobileCountKey, out int c);
            _cache.Set(MobileCountKey, Math.Max(0, c - 1));
        }
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
    public int    UserId   { get; set; }  // 0 = guest
    public string DeviceId { get; set; } = "";
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