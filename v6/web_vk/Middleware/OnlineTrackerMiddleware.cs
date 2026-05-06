using Microsoft.Extensions.Caching.Memory;

namespace web_vk.Middleware
{
    public class OnlineTrackerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;

        public const string AnonPrefix   = "anon_visitor_";
        public const string AnonCountKey = "anon_visitor_count";

        private static readonly object _lock = new();

        public OnlineTrackerMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next  = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";

            // Bỏ qua static assets và API calls từ mobile app
            // LastActiveAt của mobile user chỉ do heartbeat endpoint cập nhật
            if (path.StartsWith("/css")    || path.StartsWith("/js")  ||
                path.StartsWith("/lib")    || path.StartsWith("/images") ||
                path.StartsWith("/favicon")|| path.StartsWith("/api"))
            {
                await _next(context);
                return;
            }

            // Chỉ theo dõi khách ẩn danh (web CMS visitors)
            // Không cập nhật LastActiveAt cho admin CMS — tránh tự đếm mình
            var userId = context.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
            {
                var visitorId    = context.Session.GetString("visitorId");
                bool isNewVisitor = string.IsNullOrEmpty(visitorId);

                if (isNewVisitor)
                {
                    visitorId = Guid.NewGuid().ToString();
                    context.Session.SetString("visitorId", visitorId);
                }

                string cacheKey = AnonPrefix + visitorId;
                bool wasActive  = _cache.TryGetValue(cacheKey, out _);

                _cache.Set(cacheKey, DateTime.Now, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromSeconds(5),
                    PostEvictionCallbacks =
                    {
                        new PostEvictionCallbackRegistration
                        {
                            EvictionCallback = (key, value, reason, state) =>
                            {
                                if (reason == EvictionReason.Expired ||
                                    reason == EvictionReason.Capacity)
                                    DecrementAnonCount((IMemoryCache)state!);
                            },
                            State = _cache
                        }
                    }
                });

                if (!wasActive)
                    IncrementAnonCount(_cache);
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
                cache.Set(AnonCountKey, Math.Max(0, current - 1));
            }
        }
    }

    public static class OnlineTrackerMiddlewareExtensions
    {
        public static IApplicationBuilder UseOnlineTracker(this IApplicationBuilder builder)
            => builder.UseMiddleware<OnlineTrackerMiddleware>();
    }
}
