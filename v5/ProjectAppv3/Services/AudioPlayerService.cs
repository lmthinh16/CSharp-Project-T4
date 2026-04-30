using System;
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
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private readonly HttpClient _httpClient = new();
        private Plugin.Maui.Audio.IAudioPlayer? _player;

        public event Action<AudioGuide?>? OnTrackChanged;
        public event Action<bool>? OnPlaybackStateChanged;

        public AudioGuide? CurrentTrack => _currentTrack;
        public bool IsPlaying => _currentTrack != null && !_isPaused && !_isInterrupted;

        public async Task PlayAsync(AudioGuide guide)
        {
            await _semaphore.WaitAsync();
            try
            {
                // 1. Dừng bài cũ
                StopInternal();

                _currentTrack = guide;
                _isPaused = false;
                _isInterrupted = false;
                
                OnTrackChanged?.Invoke(_currentTrack);
                OnPlaybackStateChanged?.Invoke(true);

                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                // Tier 1: Stream MP3 nếu có Link hợp lệ
                if (!string.IsNullOrEmpty(guide.FilePath) && guide.FilePath.StartsWith("http"))
                {
                    var stream = await _httpClient.GetStreamAsync(guide.FilePath, token);
                    _player = Plugin.Maui.Audio.AudioManager.Current.CreatePlayer(stream);
                    
                    var tcs = new TaskCompletionSource();
                    _player.PlaybackEnded += (s, e) => tcs.TrySetResult();
                    token.Register(() => { 
                        _player?.Stop(); 
                        tcs.TrySetCanceled(); 
                    });
                    
                    _player.Play();
                    await tcs.Task;
                }
                // Tier 2: Đọc TextToSpeech
                else if (!string.IsNullOrEmpty(guide.TextContent) || guide.IsGeneratedByTTS)
                {
                    var textToRead = !string.IsNullOrEmpty(guide.TextContent) ? guide.TextContent : guide.Title;
                    
                    // Tìm Locale phù hợp với LanguageCode (ví dụ: "vi-VN", "en-US")
                    var locales = await TextToSpeech.Default.GetLocalesAsync();
                    var locale = locales.FirstOrDefault(l => 
                        l.Language.Equals(guide.LanguageCode, StringComparison.OrdinalIgnoreCase) ||
                        l.Language.StartsWith(guide.LanguageCode.Split('-')[0], StringComparison.OrdinalIgnoreCase));

                    await TextToSpeech.Default.SpeakAsync(textToRead, new SpeechOptions
                    {
                        Locale = locale
                    }, token);
                }
                else
                {
                    int delayMs = guide.DurationSeconds > 0 ? guide.DurationSeconds * 1000 : 5000;
                    await Task.Delay(delayMs, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioPlayerService] Error: {ex.Message}");
            }
            finally
            {
                CleanUpPlayer();
                
                if (_currentTrack == guide && !_isPaused)
                {
                    _currentTrack = null;
                    OnTrackChanged?.Invoke(null);
                    OnPlaybackStateChanged?.Invoke(false);
                }
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Phát audio cho POI theo ngôn ngữ.
        /// Tự tìm AudioGuide phù hợp trong DB (theo LanguageCode), fallback về vi-VN.
        /// </summary>
        public async Task PlayAudioAsync(Models.Restaurant poi, string lang = "vi")
        {
            try
            {
                // Map lang code → LanguageCode trong DB
                var langCode = lang switch
                {
                    "en" => "en-US",
                    "zh" => "zh-CN",
                    "ja" => "ja-JP",
                    "ko" => "ko-KR",
                    _    => "vi-VN"
                };

                var guides = await App.Database.GetAudioGuidesAsync(poi.Id);

                // Ưu tiên đúng ngôn ngữ, fallback vi-VN, fallback bất kỳ
                var guide = guides.FirstOrDefault(g => g.LanguageCode.StartsWith(lang, StringComparison.OrdinalIgnoreCase))
                         ?? guides.FirstOrDefault(g => g.LanguageCode == "vi-VN")
                         ?? guides.FirstOrDefault();

                if (guide != null)
                {
                    await PlayAsync(guide);
                    return;
                }

                // Fallback: TTS từ script trong Restaurant model
                var script = poi.GetTtsScript(lang);
                if (!string.IsNullOrEmpty(script))
                {
                    var synthGuide = new Models.AudioGuide
                    {
                        RestaurantId    = poi.Id,
                        Title           = poi.Name,
                        TextContent     = script,
                        LanguageCode    = langCode,
                        IsGeneratedByTTS = true
                    };
                    await PlayAsync(synthGuide);
                }
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
                _player.Pause();
            }
            else
            {
                // TTS không hỗ trợ Pause -> Phải Stop và Resume sẽ phát lại từ đầu (đơn giản hóa)
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
                // Đối với TTS: Phát lại từ đầu
                _ = PlayAsync(_currentTrack);
            }
        }

        public void Stop()
        {
            _semaphore.Wait();
            try
            {
                StopInternal();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void StopInternal()
        {
            CleanUpPlayer();
            
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            
            _currentTrack = null;
            _isPaused = false;
            OnTrackChanged?.Invoke(null);
            OnPlaybackStateChanged?.Invoke(false);
        }

        private void CleanUpPlayer()
        {
            if (_player != null)
            {
                _player.Stop();
                _player.Dispose();
                _player = null;
            }
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

        public void Dispose()
        {
            Stop();
            _httpClient.Dispose();
            _semaphore.Dispose();
        }
    }
}


