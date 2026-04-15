using EdgeTtsSharp;

public class TextToSpeechService
{
    private readonly IWebHostEnvironment _env;

    public TextToSpeechService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> GenerateAudioAsync(string text, string fileName, string voiceName = "vi-VN-HoaiMyNeural")
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", "audio");
        Directory.CreateDirectory(folder);

        var fullPath = Path.Combine(folder, fileName);

        // Timeout 30 giây để tránh treo vô tận
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            var voice = await EdgeTts.GetVoice(voiceName).WaitAsync(cts.Token);
            await voice.SaveAudioToFile(text, fullPath).WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new Exception("Timeout: Không thể kết nối đến máy chủ Microsoft TTS. Kiểm tra lại kết nối mạng.");
        }

        return $"/uploads/audio/{fileName}";
    }
}