using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ProjectApp.Models;

namespace ProjectApp.Services
{
    /// <summary>
    /// Gọi ASP.NET Core backend API.
    /// Singleton — dùng ApiService.Instance hoặc inject qua DI.
    /// </summary>
    public class ApiService
    {
        // ── Singleton ─────────────────────────────────────────────
        private static ApiService? _instance;
        public static ApiService Instance => _instance ??= new ApiService();

        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // ── Đổi URL này khi deploy ────────────────────────────────
        // Test LAN (điện thoại + máy tính cùng WiFi):
        //   1. Mở CMD → gõ "ipconfig" → lấy IPv4 (vd: 192.168.1.5)
        //   2. Dùng http (không phải https) vì cert localhost không hợp lệ trên điện thoại
        // Production: đổi thành domain thật, vd: "https://vinhkhanh.com"
        public string BaseUrl { get; set; } = "http://192.168.0.106:7190"; // ← ĐỔI IP NÀY

        public ApiService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        // ── Restaurants ───────────────────────────────────────────

        public async Task<List<Restaurant>> GetRestaurantsAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<Restaurant>>(
                    $"{BaseUrl}/api/restaurants", _json);
                return result ?? [];
            }
            catch (Exception ex) { Debug($"GetRestaurants: {ex.Message}"); return []; }
        }

        public async Task<Restaurant?> GetRestaurantByIdAsync(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<Restaurant>(
                    $"{BaseUrl}/api/restaurants/{id}", _json);
            }
            catch (Exception ex) { Debug($"GetRestaurantById: {ex.Message}"); return null; }
        }

        /// <summary>Lấy danh sách POI gần vị trí user trong bán kính radius (mét).</summary>
        public async Task<List<Restaurant>> GetNearbyAsync(double lat, double lng, int radiusMeters = 2000)
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<Restaurant>>(
                    $"{BaseUrl}/api/restaurants/nearby?lat={lat}&lng={lng}&radius={radiusMeters}", _json);
                return result ?? [];
            }
            catch (Exception ex) { Debug($"GetNearby: {ex.Message}"); return []; }
        }

        // ── Tours ─────────────────────────────────────────────────

        public async Task<List<Tour>> GetToursAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<Tour>>(
                    $"{BaseUrl}/api/tours", _json);
                return result ?? [];
            }
            catch (Exception ex) { Debug($"GetTours: {ex.Message}"); return []; }
        }

        // ── Languages ─────────────────────────────────────────────

        public async Task<List<AppLanguage>> GetLanguagesAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<AppLanguage>>(
                    $"{BaseUrl}/api/languages", _json);
                return result ?? DefaultLanguages();
            }
            catch (Exception ex) { Debug($"GetLanguages: {ex.Message}"); return DefaultLanguages(); }
        }

        // ── Auth ──────────────────────────────────────────────────

        public async Task<LoginResult> LoginWithDetailsAsync(string username, string password)
        {
            try
            {
                var payload  = new { username, password };
                var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/auth/login", payload);

                if (!response.IsSuccessStatusCode)
                    return new LoginResult(false, "Sai tài khoản hoặc mật khẩu.");

                var result = await response.Content.ReadFromJsonAsync<LoginResult>(_json);
                return result ?? new LoginResult(false, "Phản hồi không hợp lệ.");
            }
            catch (Exception ex)
            {
                Debug($"Login: {ex.Message}");
                return new LoginResult(false, "Không thể kết nối đến máy chủ.");
            }
        }

        // ── Analytics ─────────────────────────────────────────────

        /// <summary>Ghi sự kiện analytics lên server (overload đầy đủ tham số).</summary>
        public async Task<bool> PostAnalyticAsync(
            int poiId, string eventType,
            double lat = 0, double lng = 0, double value = 0)
        {
            try
            {
                var payload = new { poiId, eventType, lat, lng, value };
                var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/analytics", payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex) { Debug($"PostAnalytic: {ex.Message}"); return false; }
        }

        /// <summary>Ghi GPS point ẩn danh lên server để vẽ heatmap.</summary>
        public async Task PostGpsPointAsync(double lat, double lng)
        {
            try
            {
                var payload = new { lat, lng };
                await _http.PostAsJsonAsync($"{BaseUrl}/api/analytics/gps", payload);
            }
            catch (Exception ex) { Debug($"PostGps: {ex.Message}"); }
        }

        /// <summary>Xóa toàn bộ analytics của user trên server.</summary>
        public async Task ClearAnalyticsOnServerAsync()
        {
            try { await _http.DeleteAsync($"{BaseUrl}/api/analytics"); }
            catch (Exception ex) { Debug($"ClearAnalytics: {ex.Message}"); }
        }

        // ── Booking ───────────────────────────────────────────────

        public async Task<bool> PostBookingAsync(Booking booking)
        {
            try
            {
                var payload = new
                {
                    restaurantId  = booking.RestaurantId,
                    customerName  = booking.CustomerName,
                    customerPhone = booking.CustomerPhone,
                    guestCount    = booking.GuestCount,
                    bookingDate   = booking.BookingDate,
                    bookingTime   = booking.BookingTime,
                    note          = booking.Note,
                    paymentMethod = booking.PaymentMethod,
                    paymentStatus = booking.PaymentStatus,
                    depositAmount = booking.DepositAmount,
                    bookingCode   = booking.BookingCode
                };
                var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/bookings", payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex) { Debug($"PostBooking: {ex.Message}"); return false; }
        }

        // ── Health ────────────────────────────────────────────────

        public async Task<bool> PingAsync()
        {
            try
            {
                var response = await _http.GetAsync($"{BaseUrl}/api/health");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        /// <summary>Ping server để duy trì trạng thái online. Gọi mỗi 2 phút.</summary>
        public async Task SendHeartbeatAsync(int userId)
        {
            try
            {
                var payload = new { userId };
                await _http.PostAsJsonAsync($"{BaseUrl}/api/heartbeat", payload);
            }
            catch { /* Bỏ qua lỗi heartbeat – không critical */ }
        }

        // ── Helpers ───────────────────────────────────────────────

        private static List<AppLanguage> DefaultLanguages() =>
        [
            new AppLanguage { Code = "vi", Name = "Tiếng Việt", Flag = "🇻🇳", IsDefault = true,  SortOrder = 0 },
            new AppLanguage { Code = "en", Name = "English",    Flag = "🇺🇸", IsDefault = false, SortOrder = 1 },
            new AppLanguage { Code = "zh", Name = "中文",        Flag = "🇨🇳", IsDefault = false, SortOrder = 2 },
        ];

        private static void Debug(string msg)
            => System.Diagnostics.Debug.WriteLine($"[ApiService] {msg}");
    }

    // ── DTOs ──────────────────────────────────────────────────────

    public record LoginResult(
        bool   Success,
        string Message  = "",
        int    UserId   = 0,
        string Role     = "user",
        string FullName = "",
        string Username = ""
    );
}
