using EdgeTtsSharp;
using web_vk.Models;

namespace web_vk.Services;

public class EdgeTextToSpeechService
{
    private readonly string _audioFolder;
    private readonly ILogger<EdgeTextToSpeechService> _logger;

    public EdgeTextToSpeechService(IWebHostEnvironment env, ILogger<EdgeTextToSpeechService> logger)
    {
        _audioFolder = Path.Combine(env.WebRootPath, "uploads", "audio");
        Directory.CreateDirectory(_audioFolder);
        _logger = logger;
    }

    public async Task<Audio> GenerateAudioAsync(string text, string voice = "vi-VN-HoaiMyNeural", int? restaurantId = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text không được để trống");

        var fileName = $"{Guid.NewGuid()}.mp3";
        var fullPath = Path.Combine(_audioFolder, fileName);

        // Timeout 30 giây để tránh treo vô tận
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            var voiceObj = await EdgeTts.GetVoice(voice).WaitAsync(cts.Token);
            await voiceObj.SaveAudioToFile(text, fullPath).WaitAsync(cts.Token);

            var audio = new Audio
            {
                Title = text.Length > 100 ? text.Substring(0, 97) + "..." : text,
                FileName = fileName,
                FilePath = $"/uploads/audio/{fileName}",
                TextContent = text,
                VoiceName = voice,
                UploadedAt = DateTime.Now,
                RestaurantId = restaurantId,
                IsGeneratedByTTS = true
            };

            _logger.LogInformation("✅ Tạo audio TTS thành công: {FileName}", fileName);
            return audio;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("❌ Timeout khi tạo TTS");
            throw new Exception("Timeout: Không thể kết nối đến máy chủ Microsoft TTS. Kiểm tra lại kết nối mạng.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi generate TTS");
            throw;
        }
    }

    public List<string> GetAvailableVoices()
    {
        return new List<string>
        {
            "vi-VN-HoaiMyNeural",
            "vi-VN-NamMinhNeural",
            "vi-VN-ThanhMaiNeural"
        };
    }
}
