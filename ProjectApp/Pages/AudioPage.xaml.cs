using ProjectApp.Models;
using ProjectApp.Services;

namespace ProjectApp.Pages
{
    public partial class AudioPage : ContentPage
    {
        private readonly AudioQueueService _queue = App.AudioQueue;

        public AudioPage()
        {
            InitializeComponent();

            _queue.OnTrackChanged += OnTrackChanged;
            _queue.OnPlaybackStateChanged += OnPlaybackStateChanged;
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
                RefreshQueue();
            });
        }

        private void OnPlaybackStateChanged(bool isPlaying)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                BtnPlayPause.Text = isPlaying ? "⏸" : "▶");
        }

        private void RefreshUI()
        {
            bool hasTrack = _queue.CurrentTrack != null;
            NowPlayingCard.IsVisible = hasTrack;
            if (hasTrack)
            {
                LblCurrentTitle.Text    = _queue.CurrentTrack!.Title;
                LblCurrentDuration.Text = _queue.CurrentTrack.DurationDisplay;
                BtnPlayPause.Text       = _queue.IsPlaying ? "⏸" : "▶";
            }
            RefreshQueue();
        }

        private void RefreshQueue()
        {
            var items = _queue.Queue.ToList();
            bool hasQueue = items.Count > 0;
            bool hasAnything = hasQueue || _queue.CurrentTrack != null;

            QueueCollection.ItemsSource = items;
            QueueCollection.IsVisible = hasQueue;
            EmptyState.IsVisible      = !hasAnything;
            BtnClear.IsVisible        = hasAnything;
        }

        private void OnPlayPauseClicked(object sender, EventArgs e)
        {
            if (_queue.IsPlaying) _queue.Pause();
            else _queue.Resume();
        }

        private void OnSkipClicked(object sender, EventArgs e)
            => _queue.SkipCurrent();

        private void OnClearClicked(object sender, EventArgs e)
        {
            _queue.ClearQueue();
            RefreshUI();
        }
    }
}
