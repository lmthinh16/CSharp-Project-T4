using Microsoft.Extensions.Caching.Memory;
using web_vk.Models;
using Microsoft.EntityFrameworkCore;

namespace web_vk.Middleware
{
    public class OnlineTrackerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;

        // Key prefix để lưu anonymous visitors trong MemoryCache
        public const string AnonPrefix = "anon_visitor_";

        public OnlineTrackerMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            // Bỏ qua các request tĩnh (css, js, ảnh...)
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/css") || path.StartsWith("/js") ||
                path.StartsWith("/lib") || path.StartsWith("/images") ||
                path.StartsWith("/favicon"))
            {
                await _next(context);
                return;
            }

            var userId = context.Session.GetString("userId");

            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int uid))
            {
                // ── Người dùng đã đăng nhập → cập nhật LastActiveAt trong DB ──
                var user = await db.Users.FindAsync(uid);
                if (user != null)
                {
                    user.LastActiveAt = DateTime.Now;
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                // ── Khách ẩn danh → dùng MemoryCache ──
                var visitorId = context.Session.GetString("visitorId");
                if (string.IsNullOrEmpty(visitorId))
                {
                    visitorId = Guid.NewGuid().ToString();
                    context.Session.SetString("visitorId", visitorId);
                }

                // Lưu vào cache, tự hết hạn sau 5 phút không hoạt động
                _cache.Set(
                    AnonPrefix + visitorId,
                    DateTime.Now,
                    new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(5)
                    }
                );
            }

            await _next(context);
        }
    }

    // Extension method để đăng ký middleware gọn hơn
    public static class OnlineTrackerMiddlewareExtensions
    {
        public static IApplicationBuilder UseOnlineTracker(this IApplicationBuilder builder)
            => builder.UseMiddleware<OnlineTrackerMiddleware>();
    }
}
