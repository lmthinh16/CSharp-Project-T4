using ProjectApp.Models;

namespace ProjectApp.Services
{
    public class AnalyticsService
    {
        private static AnalyticsService? _instance;
        public static AnalyticsService Instance => _instance ??= new AnalyticsService();

        private AnalyticsService() { }

        public static async Task RecordGpsPointAsync(double lat, double lng)
        {
            try
            {
                var evt = new AnalyticsEvent
                {
                    EventType = "gps_point",
                    Username = UserSession.Instance.Username,
                    Lat = lat,
                    Lng = lng,
                    TimestampTicks = DateTime.Now.Ticks
                };
                await App.Database.InsertAnalyticsEventAsync(evt);

                // Gửi lên server để CMS vẽ heatmap (fire-and-forget, ẩn danh)
                _ = ApiService.Instance.PostGpsPointAsync(lat, lng);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsService] RecordGps: {ex.Message}");
            }
        }

        public static async Task RecordPoiVisitAsync(int poiId, string eventType = "poi_visit", double lat = 0, double lng = 0)
        {
            try
            {
                var evt = new AnalyticsEvent
                {
                    EventType = eventType,
                    PoiId = poiId,
                    Username = UserSession.Instance.Username,
                    Lat = lat,
                    Lng = lng,
                    TimestampTicks = DateTime.Now.Ticks
                };
                await App.Database.InsertAnalyticsEventAsync(evt);
                bool success = await ApiService.Instance.PostAnalyticAsync(poiId, eventType, lat, lng);
                if (!success) System.Diagnostics.Debug.WriteLine($"[AnalyticsService] Warning: Could not sync {eventType} to server (POI: {poiId})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsService] RecordVisit: {ex.Message}");
            }
        }

        public static async Task RecordAudioPlayedAsync(int poiId, string lang, double durationSeconds)
        {
            try
            {
                var evt = new AnalyticsEvent
                {
                    EventType = $"audio_{lang}",
                    PoiId = poiId,
                    Username = UserSession.Instance.Username,
                    Value = durationSeconds,
                    TimestampTicks = DateTime.Now.Ticks
                };
                await App.Database.InsertAnalyticsEventAsync(evt);
                bool success = await ApiService.Instance.PostAnalyticAsync(poiId, $"audio_{lang}", 0, 0, durationSeconds);
                if (!success) System.Diagnostics.Debug.WriteLine($"[AnalyticsService] Warning: Could not sync Audio event to server (POI: {poiId})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsService] RecordAudio: {ex.Message}");
            }
        }

        public static async Task<List<AnalyticsEvent>> GetAllEventsAsync()
        {
            try { return await App.Database.GetAllAnalyticsEventsAsync(); }
            catch { return []; }
        }

        public static async Task<int> GetTotalVisitsAsync()
        {
            try
            {
                var events = await App.Database.GetAnalyticsEventsAsync("poi_visit");
                return events.Count;
            }
            catch { return 0; }
        }

        /// <summary>
        /// Trả về top N POI dưới dạng List PoiStats — dùng cho AnalyticsPage.
        /// </summary>
        public static async Task<List<PoiStats>> GetTopPoisAsync(int topN = 5)
        {
            try
            {
                // 1. Lấy tất cả sự kiện và lọc các ID hợp lệ (>0) bằng Username
                var currentUser = UserSession.Instance.Username;
                var allEvents = await App.Database.GetAllAnalyticsEventsAsync();
                var visitEvents = allEvents.Where(e => e.PoiId > 0 && e.Username == currentUser &&
                    (e.EventType == "poi_visit" || e.EventType.StartsWith("audio_"))).ToList();

                if (visitEvents.Count == 0) return [];

                // 2. Lấy thông tin quán ăn để map tên
                var restaurants = await App.Database.GetRestaurantsAsync();

                // Nếu local DB thiếu thông tin, sync nhanh từ API
                var uniqueIds = visitEvents.Select(e => e.PoiId).Distinct().ToList();
                bool needsSync = uniqueIds.Any(id => !restaurants.Any(r => r.Id == id)) || restaurants.Count == 0;

                if (needsSync)
                {
                    try
                    {
                        var apiList = await ApiService.Instance.GetRestaurantsAsync();
                        if (apiList != null && apiList.Count > 0)
                        {
                            foreach (var r in apiList) await App.Database.SaveRestaurantAsync(r);
                            restaurants = apiList;
                        }
                    }
                    catch { }
                }

                // 3. Phân tích top POI
                var grouped = visitEvents
                    .GroupBy(e => e.PoiId)
                    .OrderByDescending(g => g.Count())
                    .Take(topN);

                var result = new List<PoiStats>();
                foreach (var g in grouped)
                {
                    var r = restaurants.FirstOrDefault(x => x.Id == g.Key);

                    // ── FIX: Tên rõ ràng thay vì số ──
                    string poiName = r?.Name ?? $"Điểm tham quan chưa rõ danh tính";

                    var audioEvents = g.Where(e => e.EventType.StartsWith("audio_")).ToList();

                    result.Add(new PoiStats
                    {
                        PoiId = g.Key,
                        PoiName = poiName,
                        ListenCount = audioEvents.Count,
                        TotalSeconds = audioEvents.Sum(e => e.Value)
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsService] GetTopPois: {ex.Message}");
                return [];
            }
        }

        /// <summary>
        /// Tổng hợp thống kê — trả về object với đúng tên property mà AnalyticsPage dùng
        /// </summary>
        public static async Task<AnalyticsSummary> GetSummaryAsync()
        {
            try
            {
                var currentUser = UserSession.Instance.Username;
                var allEvents = await App.Database.GetAllAnalyticsEventsAsync();
                var userEvents = allEvents.Where(e => e.Username == currentUser).ToList();

                int totalListens = userEvents.Count(e => e.PoiId > 0 && e.EventType.StartsWith("audio_"));
                int uniquePois = userEvents.Where(e => e.PoiId > 0 && e.EventType.StartsWith("audio_"))
                                            .Select(e => e.PoiId).Distinct().Count();
                double totalSec = userEvents.Where(e => e.PoiId > 0 && e.EventType.StartsWith("audio_"))
                                            .Sum(e => e.Value);
                double avgSec = totalListens > 0 ? totalSec / totalListens : 0;
                int tourCompletes = userEvents.Count(e => e.EventType == "tour_complete");

                return new AnalyticsSummary
                {
                    TotalListens = totalListens,
                    UniquePois = uniquePois,
                    AvgListenSec = avgSec,
                    TourCompletes = tourCompletes
                };
            }
            catch
            {
                return new AnalyticsSummary();
            }
        }

        public static async Task ClearAllAsync()
        {
            try
            {
                await App.Database.ClearAnalyticsAsync();
                await ApiService.Instance.ClearAnalyticsOnServerAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyticsService] Clear: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Model tổng hợp thống kê
    /// </summary>
    public class AnalyticsSummary
    {
        public int TotalListens { get; set; }
        public int UniquePois { get; set; }
        public double AvgListenSec { get; set; }
        public int TourCompletes { get; set; }
    }
}
