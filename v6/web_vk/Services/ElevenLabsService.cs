using System.Text;
using System.Text.Json;

namespace web_vk.Services;

public class ElevenLabsService
{
    private readonly IWebHostEnvironment _env;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ElevenLabsService(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        _apiKey = config["ElevenLabs:ApiKey"];
    }

    public async Task<string> GenerateAudioAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new Exception("Text rỗng");

        var folder = Path.Combine(_env.WebRootPath, "uploads", "audio");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid()}.mp3";
        var fullPath = Path.Combine(folder, fileName);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.elevenlabs.io/v1/text-to-speech/EXAVITQu4vr4xnSDxMaL"
        );

        request.Headers.Add("xi-api-key", _apiKey);

        var body = new
        {
            text = text,
            model_id = "eleven_multilingual_v2"
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception("ElevenLabs lỗi: " + err);
        }

        var audioBytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(fullPath, audioBytes);

        return $"/uploads/audio/{fileName}";
    }
}