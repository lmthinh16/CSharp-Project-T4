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

        // FIX: Key lưu tổng số anonymous visitor active (thay thế reflection vào _entries)
        public const string AnonCountKey = "anon_visitor_count";

        // Lock object để thread-safe khi cập nhật counter
        private static readonly object _lock = new();

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
                bool isNewVisitor = string.IsNullOrEmpty(visitorId);

                if (isNewVisitor)
                {
                    visitorId = Guid.NewGuid().ToString();
                    context.Session.SetString("visitorId", visitorId);
                }

                string cacheKey = AnonPrefix + visitorId;
                bool wasActive = _cache.TryGetValue(cacheKey, out _);

                // Lưu visitor entry, tự hết hạn sau 5 phút không hoạt động
                _cache.Set(
                    cacheKey,
                    DateTime.Now,
                    new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(5),
                        PostEvictionCallbacks =
                        {
                            // FIX: Khi entry hết hạn (visitor offline) → giảm counter
                            new PostEvictionCallbackRegistration
                            {
                                EvictionCallback = (key, value, reason, state) =>
                                {
                                    if (reason == EvictionReason.Expired ||
                                        reason == EvictionReason.Capacity)
                                    {
                                        DecrementAnonCount((IMemoryCache)state!);
                                    }
                                },
                                State = _cache
                            }
                        }
                    }
                );

                // Nếu visitor mới (chưa có trong cache) → tăng counter
                if (!wasActive)
                {
                    IncrementAnonCount(_cache);
                }
            }

            await _next(context);
        }

        private static void IncrementAnonCount(IMemoryCache cache)
        {
            lock (_lock)
            {
                cache.TryGetValue(AnonCountKey, out int current);
                cache.Set(AnonCountKey, current + 1);
            }
        }

        private static void DecrementAnonCount(IMemoryCache cache)
        {
            lock (_lock)
            {
                cache.TryGetValue(AnonCountKey, out int current);
                int newVal = Math.Max(0, current - 1);
                cache.Set(AnonCountKey, newVal);
            }
        }
    }

    // Extension method để đăng ký middleware
    public static class OnlineTrackerMiddlewareExtensions
    {
        public static IApplicationBuilder UseOnlineTracker(this IApplicationBuilder builder)
            => builder.UseMiddleware<OnlineTrackerMiddleware>();
    }
}