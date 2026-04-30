using ProjectApp.Models;
using ProjectApp.Services;

namespace ProjectApp.Pages
{
    public partial class AudioPage : ContentPage
    {
        private readonly IAudioService _audio = App.Audio;

        public AudioPage()
        {
            InitializeComponent();

            _audio.OnTrackChanged += OnTrackChanged;
            _audio.OnPlaybackStateChanged += OnPlaybackStateChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            RefreshUI();
        }

        private void OnTrackChanged(AudioGuide? track)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                bool hasTrack = track != null;
                NowPlayingCard.IsVisible = hasTrack;

                if (hasTrack)
                {
                    LblCurrentTitle.Text    = track!.Title;
                    LblCurrentDuration.Text = track.DurationDisplay;
                }
                
                // 2-Tier Audio: Hiện tại không dùng queue chung
                QueueCollection.IsVisible = false;
                EmptyState.IsVisible      = !hasTrack;
                BtnClear.IsVisible        = hasTrack;
            });
        }

        private void OnPlaybackStateChanged(bool isPlaying)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                BtnPlayPause.Text = isPlaying ? "⏸" : "▶");
        }

        private void RefreshUI()
        {
            var track = _audio.CurrentTrack;
            bool hasTrack = track != null;
            
            NowPlayingCard.IsVisible = hasTrack;
            if (hasTrack)
            {
                LblCurrentTitle.Text    = track!.Title;
                LblCurrentDuration.Text = track.DurationDisplay;
                BtnPlayPause.Text       = _audio.IsPlaying ? "⏸" : "▶";
            }

            QueueCollection.IsVisible = false; // Bỏ queue trong bản 2-Tier manual
            EmptyState.IsVisible      = !hasTrack;
            BtnClear.IsVisible        = hasTrack;
        }

        private void OnPlayPauseClicked(object sender, EventArgs e)
        {
            if (_audio.IsPlaying) _audio.Pause();
            else _audio.Resume();
        }

        private void OnSkipClicked(object sender, EventArgs e)
        {
            // Manual mode: Dừng bài hiện tại
            _audio.Stop();
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            _audio.Stop();
            RefreshUI();
        }
    }
}

