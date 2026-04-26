using SQLite;
using System.Text.Json;

namespace ProjectApp.Models
{
    [Table("restaurants")]
    public class Restaurant
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [Indexed]
        public string Category { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("lng")]
        public double Longitude { get; set; }
        public string Address { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("imagePath")]
        public string ImageUrl { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string OpenHours { get; set; } = string.Empty;

        // ── TTS scripts theo ngôn ngữ cố định ────────────────────
        [System.Text.Json.Serialization.JsonPropertyName("ttsScript")]
        public string TtsScript { get; set; } = string.Empty; // vi

        [System.Text.Json.Serialization.JsonPropertyName("ttsScriptEn")]
        public string TtsScriptEn { get; set; } = string.Empty; // en

        [System.Text.Json.Serialization.JsonPropertyName("ttsScriptZh")]
        public string TtsScriptZh { get; set; } = string.Empty; // zh

        // ── Dynamic languages: JSON {"ja":{"name":"...","tts":"..."}} ──
        public string Translations { get; set; } = "{}";

        // ── Geofencing ────────────────────────────────────────────
        public int Radius { get; set; } = 50; // metres, default 50m theo PRD

        // ── Quảng cáo popup ──────────────────────────────────────
        public bool IsAdsPopup { get; set; } = false;

        // ── Audio file URL (pre-recorded thay vì TTS) ────────────
        public string AudioUrl { get; set; } = string.Empty;

        // ── Yêu thích ─────────────────────────────────────────────
        public bool IsFavorite { get; set; } = false;

        // ── Audit ─────────────────────────────────────────────────
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // ── Runtime only (không lưu DB) ───────────────────────────
        [Ignore] public double? DistanceMeters { get; set; }
        [Ignore] public bool IsNearest { get; set; }

        // Mảng chứa các Audios hứng từ Json trả về của CMS API
        [Ignore]
        [System.Text.Json.Serialization.JsonPropertyName("audios")]
        public List<AudioGuide> Audios { get; set; } = new();

        [Ignore]
        public string CategoryEmoji => Category switch
        {
            "Quán ăn" => "🍜",
            "Cà phê" => "☕",
            "Tráng miệng" => "🍮",
            _ => "📍"
        };

        // ── Helper: lấy TTS script theo ngôn ngữ ─────────────────
        // Fallback chain: target lang → vi → empty
        public string GetTtsScript(string lang = "vi")
        {
            // 1. Thử dynamic translations JSON trước
            if (!string.IsNullOrEmpty(Translations) && Translations != "{}")
            {
                try
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Translations);
                    if (dict != null && dict.TryGetValue(lang, out var entry))
                    {
                        var tts = entry.GetProperty("tts").GetString();
                        if (!string.IsNullOrEmpty(tts)) return tts;
                    }
                }
                catch { /* JSON lỗi → fallthrough */ }
            }

            // 2. Built-in 3 ngôn ngữ
            return lang switch
            {
                "en" => string.IsNullOrEmpty(TtsScriptEn) ? TtsScript : TtsScriptEn,
                "zh" => string.IsNullOrEmpty(TtsScriptZh) ? TtsScript : TtsScriptZh,
                _ => TtsScript
            };
        }
    }

    // ────────────────────────────────────────────────────────────

    [Table("tours")]
    public class Tour
    {
        [PrimaryKey]
        public int Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string Name { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameZh { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DescEn { get; set; } = string.Empty;
        public string DescZh { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Tạm mượn đối tượng ẩn để hứng Stops từ CMS API 
        public class TourStopTemp
        {
            public class RestTemp { public int Id { get; set; } }
            public RestTemp Restaurant { get; set; } = new();
        }

        [Ignore]
        [System.Text.Json.Serialization.JsonPropertyName("stops")]
        public List<TourStopTemp> Stops 
        { 
            get => new(); 
            set 
            {
                if (value != null && value.Count > 0)
                {
                    var ids = value.Select(s => s.Restaurant.Id).ToList();
                    Pois = JsonSerializer.Serialize(ids);
                }
            } 
        }

        // Pois = JSON array of restaurant IDs
        public string Pois { get; set; } = "[]";

        [Ignore]
        public List<int> RestaurantIds
        {
            get => JsonSerializer.Deserialize<List<int>>(Pois) ?? [];
            set => Pois = JsonSerializer.Serialize(value ?? []);
        }

        // i18n helper
        public string GetName(string lang) => lang switch
        {
            "en" => string.IsNullOrEmpty(NameEn) ? Name : NameEn,
            "zh" => string.IsNullOrEmpty(NameZh) ? Name : NameZh,
            _ => Name
        };

        public string GetDescription(string lang) => lang switch
        {
            "en" => string.IsNullOrEmpty(DescEn) ? Description : DescEn,
            "zh" => string.IsNullOrEmpty(DescZh) ? Description : DescZh,
            _ => Description
        };
    }

    // ────────────────────────────────────────────────────────────

    [Table("visit_history")]
    public class VisitHistory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int RestaurantId { get; set; }

        public DateTimeOffset VisitedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    // ────────────────────────────────────────────────────────────

    [Table("audio_guides")]
    public class AudioGuide
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int RestaurantId { get; set; }

        public string Title { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("textContent")]
        public string TextContent { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("languageCode")]
        public string LanguageCode { get; set; } = "vi-VN";
        
        [System.Text.Json.Serialization.JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("isGeneratedByTTS")]
        public bool IsGeneratedByTTS { get; set; } = false;

        public int DurationSeconds { get; set; }
        public int SortOrder { get; set; }

        [Ignore]
        public string DurationDisplay =>
            $"{DurationSeconds / 60}:{DurationSeconds % 60:D2}";
    }

    // ────────────────────────────────────────────────────────────

    [Table("app_languages")]
    public class AppLanguage
    {
        [PrimaryKey]
        public string Code { get; set; } = string.Empty; // "vi", "en", "zh", "ja"...
        public string Name { get; set; } = string.Empty; // "Tiếng Việt"
        public string Flag { get; set; } = string.Empty; // "🇻🇳"
        public bool IsDefault { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }

    // ────────────────────────────────────────────────────────────

    [Table("users")]
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // hash khi cần
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "user"; // "admin" | "owner" | "user"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
