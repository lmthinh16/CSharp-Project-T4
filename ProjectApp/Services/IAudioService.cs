using ProjectApp.Models;

namespace ProjectApp.Services
{
    public interface IAudioService
    {
        AudioGuide? CurrentTrack { get; }
        bool IsPlaying { get; }
        
        event Action<AudioGuide?>? OnTrackChanged;
        event Action<bool>? OnPlaybackStateChanged;

        Task PlayAsync(AudioGuide guide);
        void Pause();
        void Resume();
        void Stop();
        void InterruptForNotification();
        void ResumeAfterInterrupt();
    }
}
