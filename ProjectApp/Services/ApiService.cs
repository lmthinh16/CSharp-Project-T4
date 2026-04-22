using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ProjectApp.Models;

namespace ProjectApp.Services
{
    /// <summary>
    /// Gọi ASP.NET Core backend API.
    /// Dùng singleton HttpClient — không tạo mới mỗi request.
    /// BaseUrl đổi sang URL deploy thực tế khi production.
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // ── Đổi URL này khi deploy ────────────────────────────────
        public string BaseUrl { get; set; } = "http://10.0.2.2:5000"; // Android emulator → localhost

        public ApiService()
        {
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
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
            catch (Exception ex)
            {
                Debug($"GetRestaurants: {ex.Message}");
                return [];
            }
        }

        public async Task<Restaurant?> GetRestaurantByIdAsync(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<Restaurant>(
                    $"{BaseUrl}/api/restaurants/{id}", _json);
            }
            catch (Exception ex)
            {
                Debug($"GetRestaurantById: {ex.Message}");
                return null;
            }
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
            catch (Exception ex)
            {
                Debug($"GetTours: {ex.Message}");
                return [];
            }
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
            catch (Exception ex)
            {
                Debug($"GetLanguages: {ex.Message}");
                return DefaultLanguages(); // fallback nếu offline
            }
        }

        // ── Auth ──────────────────────────────────────────────────

        public async Task<LoginResult> LoginWithDetailsAsync(string username, string password)
        {
            try
            {
                var payload = new { username, password };
                var response = await _http.PostAsJsonAsync(
                    $"{BaseUrl}/api/auth/login", payload);

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

        public async Task PostAnalyticAsync(int restaurantId, string eventType)
        {
            try
            {
                var payload = new { restaurantId, eventType };
                await _http.PostAsJsonAsync($"{BaseUrl}/api/analytics", payload);
            }
            catch (Exception ex)
            {
                Debug($"Analytics: {ex.Message}"); // silent fail
            }
        }

        // ── Helpers ───────────────────────────────────────────────

        private static List<AppLanguage> DefaultLanguages() =>
        [
            new AppLanguage { Code = "vi", Name = "Tiếng Việt", Flag = "🇻🇳", IsDefault = true, SortOrder = 0 },
            new AppLanguage { Code = "en", Name = "English",    Flag = "🇺🇸", IsDefault = false, SortOrder = 1 },
            new AppLanguage { Code = "zh", Name = "中文",        Flag = "🇨🇳", IsDefault = false, SortOrder = 2 },
        ];

        private static void Debug(string msg)
            => System.Diagnostics.Debug.WriteLine($"[ApiService] {msg}");
    }

    // ── DTOs ──────────────────────────────────────────────────────

    public record LoginResult(
        bool   Success,
        string Message   = "",
        int    UserId    = 0,
        string Role      = "user",
        string FullName  = "",
        string Username  = ""
    );
}
