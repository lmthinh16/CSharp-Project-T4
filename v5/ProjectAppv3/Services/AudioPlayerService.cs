using ProjectApp.Models;
using Microsoft.Maui.Media;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectApp.Services
{
    public class AudioPlayerService : IAudioService, IDisposable
    {
        private AudioGuide? _currentTrack;
        private CancellationTokenSource? _cts;
        private bool _isPaused;
        private bool _isInterrupted;

        // Version counter — mỗi lần PlayAsync mới bắt đầu sẽ tăng lên 1.
        // finally block dùng nó để biết mình có còn là session mới nhất không.
        private volatile int _playVersion;

        private readonly HttpClient _httpClient = new();
        private Plugin.Maui.Audio.IAudioPlayer? _player;

        public event Action<AudioGuide?>? OnTrackChanged;
        public event Action<bool>? OnPlaybackStateChanged;

        public AudioGuide? CurrentTrack => _currentTrack;
        public bool IsPlaying => _currentTrack != null && !_isPaused && !_isInterrupted;

        public async Task PlayAsync(AudioGuide guide)
        {
            // 1. Huỷ bài đang phát (nếu có) trước khi bắt đầu bài mới
            _cts?.Cancel();

            var version = Interlocked.Increment(ref _playVersion);
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            CleanUpPlayer();

            _currentTrack    = guide;
            _isPaused        = false;
            _isInterrupted   = false;

            OnTrackChanged?.Invoke(_currentTrack);
            OnPlaybackStateChanged?.Invoke(true);

            try
            {
                bool played = false;

                // Tier 1: Stream MP3 nếu có URL hợp lệ
                if (!string.IsNullOrEmpty(guide.FilePath) && guide.FilePath.StartsWith("http"))
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[Audio] Tier1 stream: {guide.FilePath}");
                        var stream = await _httpClient.GetStreamAsync(guide.FilePath, token);
                        token.ThrowIfCancellationRequested();

                        _player = Plugin.Maui.Audio.AudioManager.Current.CreatePlayer(stream);

                        var tcs = new TaskCompletionSource();
                        _player.PlaybackEnded += (s, e) => tcs.TrySetResult();
                        token.Register(() =>
                        {
                            _player?.Stop();
                            tcs.TrySetCanceled();
                        });

                        _player.Play();
                        await tcs.Task;
                        played = true;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Audio] Tier1 lỗi, thử TTS: {ex.Message}");
                        CleanUpPlayer();
                    }
                }

                // Tier 2: Text-To-Speech
                if (!played && (!string.IsNullOrEmpty(guide.TextContent) || guide.IsGeneratedByTTS))
                {
                    var text = !string.IsNullOrEmpty(guide.TextContent)
                        ? guide.TextContent
                        : guide.Title;

                    var locales = await TextToSpeech.Default.GetLocalesAsync();
                    var locale  = locales.FirstOrDefault(l =>
                        l.Language.Equals(guide.LanguageCode, StringComparison.OrdinalIgnoreCase) ||
                        l.Language.StartsWith(guide.LanguageCode.Split('-')[0],
                                              StringComparison.OrdinalIgnoreCase));

                    await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions { Locale = locale }, token);
                    played = true;
                }

                // Tier 3: Delay tĩnh
                if (!played)
                {
                    var ms = guide.DurationSeconds > 0 ? guide.DurationSeconds * 1000 : 5000;
                    await Task.Delay(ms, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Audio] Lỗi: {ex.Message}");
            }
            finally
            {
                CleanUpPlayer();

                // Chỉ clear state nếu đây vẫn là session mới nhất VÀ không đang pause
                if (_playVersion == version && !_isPaused)
                {
                    _currentTrack = null;
                    OnTrackChanged?.Invoke(null);
                    OnPlaybackStateChanged?.Invoke(false);
                }
            }
        }

        public async Task PlayAudioAsync(Models.Restaurant poi, string lang = "vi")
        {
            try
            {
                var langCode = lang switch
                {
                    "en" => "en-US",
                    "zh" => "zh-CN",
                    "ja" => "ja-JP",
                    "ko" => "ko-KR",
                    _    => "vi-VN"
                };

                var guides = await App.Database.GetAudioGuidesAsync(poi.Id);

                var guide = guides.FirstOrDefault(g =>
                                g.LanguageCode.StartsWith(lang, StringComparison.OrdinalIgnoreCase))
                         ?? guides.FirstOrDefault(g => g.LanguageCode == "vi-VN")
                         ?? guides.FirstOrDefault();

                // Nếu guide chỉ có FilePath (không có TTS content), enrich với script
                // để PlayAsync tự fallback sang Tier2 nếu stream thất bại
                if (guide != null && string.IsNullOrEmpty(guide.TextContent) && !guide.IsGeneratedByTTS)
                {
                    var fallback = poi.GetTtsScript(lang);
                    guide.TextContent      = !string.IsNullOrEmpty(fallback) ? fallback : poi.Name;
                    guide.IsGeneratedByTTS = true;
                }

                if (guide != null)
                {
                    await PlayAsync(guide);
                    return;
                }

                // Không có guide nào trong DB → tạo TTS thuần
                var script = poi.GetTtsScript(lang);
                await PlayAsync(new Models.AudioGuide
                {
                    RestaurantId     = poi.Id,
                    Title            = poi.Name,
                    TextContent      = !string.IsNullOrEmpty(script) ? script : poi.Name,
                    LanguageCode     = langCode,
                    IsGeneratedByTTS = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioPlayerService] PlayAudioAsync: {ex.Message}");
            }
        }

        public void Pause()
        {
            if (_currentTrack == null) return;
            _isPaused = true;

            if (_player != null)
            {
                // MP3: pause player; tcs vẫn đang chờ, không cancel CTS
                _player.Pause();
            }
            else
            {
                // TTS: cancel để dừng speak; PlayAsync sẽ exit qua OperationCanceledException
                _cts?.Cancel();
            }

            OnPlaybackStateChanged?.Invoke(false);
        }

        public void Resume()
        {
            if (!_isPaused || _isInterrupted || _currentTrack == null) return;
            _isPaused = false;

            if (_player != null)
            {
                _player.Play();
                OnPlaybackStateChanged?.Invoke(true);
            }
            else
            {
                // TTS: phát lại từ đầu
                _ = PlayAsync(_currentTrack);
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            CleanUpPlayer();
            _currentTrack  = null;
            _isPaused      = false;
            _isInterrupted = false;
            OnTrackChanged?.Invoke(null);
            OnPlaybackStateChanged?.Invoke(false);
        }

        public void InterruptForNotification()
        {
            _isInterrupted = true;
            if (_player != null) _player.Pause();
            else _cts?.Cancel();
            OnPlaybackStateChanged?.Invoke(false);
        }

        public void ResumeAfterInterrupt()
        {
            _isInterrupted = false;
            if (_currentTrack != null && !_isPaused)
            {
                if (_player != null) _player.Play();
                else _ = PlayAsync(_currentTrack);
                OnPlaybackStateChanged?.Invoke(true);
            }
        }

        private void CleanUpPlayer()
        {
            if (_player == null) return;
            _player.Stop();
            _player.Dispose();
            _player = null;
        }

        public void Dispose()
        {
            Stop();
            _httpClient.Dispose();
        }
    }
}
