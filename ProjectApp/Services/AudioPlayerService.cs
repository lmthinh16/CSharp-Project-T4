using ProjectApp.Models;

namespace ProjectApp.Services
{
    public class AudioPlayerService : IAudioService, IDisposable
    {
        private AudioGuide? _currentTrack;
        private CancellationTokenSource? _cts;
        private bool _isPaused;
        private bool _isInterrupted;

        private readonly HttpClient _httpClient = new();
        private Plugin.Maui.Audio.IAudioPlayer? _player;

        public event Action<AudioGuide?>? OnTrackChanged;
        public event Action<bool>? OnPlaybackStateChanged;

        public AudioGuide? CurrentTrack => _currentTrack;
        public bool IsPlaying => _currentTrack != null && !_isPaused && !_isInterrupted;

        public async Task PlayAsync(AudioGuide guide)
        {
            // Dừng bài cũ nếu có
            Stop();

            _currentTrack = guide;
            _isPaused = false;
            _isInterrupted = false;
            
            OnTrackChanged?.Invoke(_currentTrack);
            OnPlaybackStateChanged?.Invoke(true);

            _cts = new CancellationTokenSource();

            try
            {
                // Tier 1: Stream MP3 nếu có Link
                if (!string.IsNullOrEmpty(guide.FilePath) && guide.FilePath.StartsWith("http"))
                {
                    var stream = await _httpClient.GetStreamAsync(guide.FilePath, _cts.Token);
                    _player = Plugin.Maui.Audio.AudioManager.Current.CreatePlayer(stream);
                    
                    var tcs = new TaskCompletionSource();
                    _player.PlaybackEnded += (s, e) => tcs.TrySetResult();
                    _cts.Token.Register(() => { _player.Stop(); tcs.TrySetCanceled(); });
                    
                    _player.Play();
                    await tcs.Task;
                }
                // Tier 2: Đọc TextToSpeech nếu không có Link nhưng có Text
                else if (!string.IsNullOrEmpty(guide.TextContent) || guide.IsGeneratedByTTS)
                {
                    // Lấy text để đọc
                    var textToRead = !string.IsNullOrEmpty(guide.TextContent) ? guide.TextContent : guide.Title;
                    
                    // MAUI TextToSpeech
                    await TextToSpeech.Default.SpeakAsync(textToRead, new TextToSpeechOptions
                    {
                        // TODO: Map LanguageCode to specific Locales if needed, for now default to system
                    }, _cts.Token);
                }
                // Dự phòng: Chờ (Delay) nếu hoàn toàn không có gì
                else
                {
                    await Task.Delay(guide.DurationSeconds > 0 ? guide.DurationSeconds * 1000 : 5000, _cts.Token);
                }
            }
            catch (OperationCanceledException) { /* Bị bỏ qua/hủy bỏ do Stop() gọi CancellationToken */ }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio Playback Error: {ex.Message}");
            }
            finally
            {
                _player?.Dispose();
                _player = null;
                
                // Reset UI if it's still the same track ending naturally
                if (_currentTrack == guide)
                {
                    _currentTrack = null;
                    OnTrackChanged?.Invoke(null);
                    OnPlaybackStateChanged?.Invoke(false);
                }
            }
        }

        public void Pause()
        {
            if (_currentTrack == null) return;
            _isPaused = true;
            _player?.Pause();
            // LƯU Ý: MAUI TextToSpeech không hỗ trợ Pause() natively giữa chừng. 
            // Nếu muốn, chỉ có cách Cancel (Stop) rồi bắt đầu lại từ đầu.
            OnPlaybackStateChanged?.Invoke(false);
        }

        public void Resume()
        {
            if (!_isPaused || _isInterrupted || _currentTrack == null) return;
            _isPaused = false;
            _player?.Play();
            OnPlaybackStateChanged?.Invoke(true);
        }

        public void Stop()
        {
            _player?.Stop();
            _player?.Dispose();
            _player = null;
            
            _cts?.Cancel(); // Cancel both HttpClient and TextToSpeech
            
            _currentTrack = null;
            _isPaused = false;
            OnTrackChanged?.Invoke(null);
            OnPlaybackStateChanged?.Invoke(false);
        }

        public void InterruptForNotification()
        {
            _isInterrupted = true;
            _player?.Pause();
            OnPlaybackStateChanged?.Invoke(false);
        }

        public void ResumeAfterInterrupt()
        {
            _isInterrupted = false;
            if (_currentTrack != null && !_isPaused) 
            {
                _player?.Play();
                OnPlaybackStateChanged?.Invoke(true);
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            _httpClient.Dispose();
        }
    }
}
