using ProjectApp.Models;
using System.Threading.Tasks;

namespace ProjectApp.Services
{
    /// <summary>
    /// Interface cho Audio Service 2-Tier.
    /// Tier 1: Stream MP3 qua Plugin.Maui.Audio (khi FilePath hợp lệ).
    /// Tier 2: Text-To-Speech qua MAUI TextToSpeech.Default (khi FilePath rỗng + có TextContent).
    /// </summary>
    public interface IAudioService
    {
        // ── Trạng thái hiện tại ──────────────────────────────────

        AudioGuide? CurrentTrack { get; }
        bool IsPlaying { get; }

        // ── Sự kiện ─────────────────────────────────────────────

        event Action<AudioGuide?>? OnTrackChanged;
        event Action<bool>? OnPlaybackStateChanged;

        // ── Điều khiển ──────────────────────────────────────────

        Task PlayAsync(AudioGuide guide);

        /// <summary>Phát audio cho POI theo ngôn ngữ — tự chọn AudioGuide phù hợp.</summary>
        Task PlayAudioAsync(Models.Restaurant poi, string lang = "vi");

        void Pause();
        void Resume();
        void Stop();
    }
}

