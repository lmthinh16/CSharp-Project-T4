using System.Threading;
using ProjectApp.Models;

namespace ProjectApp.Services
{
    public class AudioQueueService
    {
        private CancellationTokenSource? _ttsCts;
        private int _currentPlayingPoiId = -1;

        public bool IsPlaying => _ttsCts != null && !_ttsCts.IsCancellationRequested;

        // "vi-VN", "en-US", "zh-CN"
        public string CurrentLanguageCode { get; set; } = "vi-VN"; 

        public int CurrentPoiId => _currentPlayingPoiId;

        public async Task PlayPoiAudioAsync(Restaurant poi)
        {
            // Nếu cùng POI đang đọc thì bỏ qua (chống spam)
            if (IsPlaying && _currentPlayingPoiId == poi.Id)
                return;

            // Dừng bài cũ
            StopAudio();

            _ttsCts = new CancellationTokenSource();
            _currentPlayingPoiId = poi.Id;

            string textToRead = "";
            var audios = await App.Database.GetAudiosForRestaurantAsync(poi.Id, CurrentLanguageCode);
            var audioRecord = audios.FirstOrDefault();
            
            if (audioRecord != null && !string.IsNullOrWhiteSpace(audioRecord.TextContent))
            {
                textToRead = audioRecord.TextContent;
            }
            else 
            {
                textToRead = $"No content available for {CurrentLanguageCode}.";
            }

            try
            {
                // Lấy 2 ký tự đầu để map (vd "vi-VN" -> "vi")
                string shortLang = CurrentLanguageCode.Length >= 2 ? CurrentLanguageCode.Substring(0, 2) : CurrentLanguageCode;
                
                var locales = await TextToSpeech.GetLocalesAsync();
                var selectedLocale = locales?.FirstOrDefault(l => l.Language.StartsWith(shortLang, StringComparison.OrdinalIgnoreCase));
                
                SpeechOptions options = null;
                if (selectedLocale != null)
                {
                    options = new SpeechOptions
                    {
                        Locale = selectedLocale
                    };
                }

                await TextToSpeech.SpeakAsync(textToRead, options, cancelToken: _ttsCts.Token);
            }
            catch (TaskCanceledException)
            {
                // Bị hủy do có bài mới hoặc Stop
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TTS Error: {ex}");
                // Lỗi TTS (không có giọng đọc, vv)
            }
            finally
            {
                if (_currentPlayingPoiId == poi.Id)
                {
                    _currentPlayingPoiId = -1;
                }
            }
        }

        public void StopAudio()
        {
            if (_ttsCts != null)
            {
                _ttsCts.Cancel();
                _ttsCts.Dispose();
                _ttsCts = null;
            }
            _currentPlayingPoiId = -1;
        }
    }
}
