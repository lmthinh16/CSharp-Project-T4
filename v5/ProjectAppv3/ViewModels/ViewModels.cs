using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectApp.Models;
using ProjectApp.Services;
using System.Collections.ObjectModel;

namespace ProjectApp.ViewModels
{
    // ════════════════════════════════════════════════════════════
    //  MainViewModel  –  MainPage
    // ════════════════════════════════════════════════════════════
    public partial class MainViewModel : ObservableObject
    {
        private readonly IAudioService _audio = App.Audio;
        private List<Restaurant> _allRestaurants = [];

        [ObservableProperty] ObservableCollection<Restaurant> _restaurants = [];
        [ObservableProperty] Restaurant? _selectedRestaurant;
        [ObservableProperty] string _searchText = "";

        // ── Audio bar bindings ────────────────────────────────────
        [ObservableProperty] bool   _hasCurrentTrack;
        [ObservableProperty] string _currentTrackTitle    = "";
        [ObservableProperty] string _currentTrackDuration = "";
        [ObservableProperty] string _playPauseIcon        = "icon_pause.png";
        [ObservableProperty] bool   _isRefreshing;

        public MainViewModel()
        {
            _audio.OnTrackChanged += track =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HasCurrentTrack = track != null;
                    CurrentTrackTitle = track?.Title ?? "";
                    var sec = (track?.DurationSeconds ?? 0);
                    CurrentTrackDuration = $"{sec / 60}:{sec % 60:D2}";
                });
            };

            _audio.OnPlaybackStateChanged += playing =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    PlayPauseIcon = playing ? "icon_pause.png" : "icon_play.png");
            };
        }

        public async Task LoadAsync()
        {
            var list = await App.Database.GetRestaurantsAsync();
            _allRestaurants = list;
            Restaurants = new ObservableCollection<Restaurant>(list);
        }

        // Được gọi khi SearchBar thay đổi text (bind 2 chiều qua SearchText)
        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
            {
                // Hiển thị lại toàn bộ POI
                Restaurants = new ObservableCollection<Restaurant>(_allRestaurants);
                return;
            }

            var filtered = _allRestaurants
                .Where(r => r.Name.Contains(value, StringComparison.OrdinalIgnoreCase)
                         || r.Address.Contains(value, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Restaurants = new ObservableCollection<Restaurant>(filtered);
        }

        [RelayCommand]
        async Task OpenDetail()
        {
            if (SelectedRestaurant == null) return;
            var r = SelectedRestaurant;
            SelectedRestaurant = null;
            await Shell.Current.GoToAsync("RestaurantDetail",
                new Dictionary<string, object> { ["Restaurant"] = r });
        }

        [RelayCommand]
        async Task Refresh()
        {
            IsRefreshing = true;
            try
            {
                await App.Sync.SyncAllAsync();
                await LoadAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        void TogglePlayPause()
        {
            if (_audio.IsPlaying) _audio.Pause();
            else _audio.Resume();
        }

        [RelayCommand]
        void ClearSearch() => SearchText = "";

        [RelayCommand]
        async Task OpenFilter()
            => await Shell.Current.DisplayAlert("Lọc", "Tính năng đang phát triển", "OK");
    }

    // ════════════════════════════════════════════════════════════
    //  AudioGuideItemVm  –  wrapper per item trong danh sách audio
    // ════════════════════════════════════════════════════════════
    public partial class AudioGuideItemVm : ObservableObject
    {
        public AudioGuide Guide { get; }

        // true = đây là track hiện tại (kể cả đang pause)
        [ObservableProperty] bool _isCurrent;
        // true = đang phát (không bị pause)
        [ObservableProperty] bool _isPlaying;

        public string PlayIcon => IsPlaying ? "⏸" : "▶";

        public Color PlayButtonColor => IsPlaying
            ? Color.FromArgb("#2563EB")
            : Color.FromArgb("#1E4A70");

        public Color RowBorderColor => IsCurrent
            ? Color.FromArgb("#2563EB")
            : Color.FromArgb("#1E3A5F");

        public Color RowBackgroundColor => IsCurrent
            ? Color.FromArgb("#091D38")
            : Color.FromArgb("#0D1B2A");

        public string LanguageFlag => Guide.LanguageCode switch
        {
            var c when c.StartsWith("en") => "🇺🇸",
            var c when c.StartsWith("zh") => "🇨🇳",
            var c when c.StartsWith("ja") => "🇯🇵",
            var c when c.StartsWith("ko") => "🇰🇷",
            _                             => "🇻🇳"
        };

        public string LanguageLabel => Guide.LanguageCode switch
        {
            var c when c.StartsWith("en") => "English",
            var c when c.StartsWith("zh") => "中文",
            var c when c.StartsWith("ja") => "日本語",
            var c when c.StartsWith("ko") => "한국어",
            _                             => "Tiếng Việt"
        };

        partial void OnIsPlayingChanged(bool _)
        {
            OnPropertyChanged(nameof(PlayIcon));
            OnPropertyChanged(nameof(PlayButtonColor));
        }

        partial void OnIsCurrentChanged(bool _)
        {
            OnPropertyChanged(nameof(RowBorderColor));
            OnPropertyChanged(nameof(RowBackgroundColor));
        }

        public AudioGuideItemVm(AudioGuide guide) => Guide = guide;
    }

    // ════════════════════════════════════════════════════════════
    //  RestaurantDetailViewModel  –  RestaurantDetailPage
    // ════════════════════════════════════════════════════════════
    public partial class RestaurantDetailViewModel : ObservableObject
    {
        private readonly IAudioService _audio = App.Audio;

        [ObservableProperty] Restaurant? _restaurant;
        [ObservableProperty] ObservableCollection<AudioGuideItemVm> _audioItems = [];
        [ObservableProperty] bool   _hasAudioGuides;
        [ObservableProperty] string _favoriteIcon  = "icon_heart.png";
        [ObservableProperty] string _favoriteLabel = "❤️ Thêm";
        [ObservableProperty] string _distanceText  = "";

        public RestaurantDetailViewModel()
        {
            // Cập nhật icon play/pause khi track thay đổi
            _audio.OnTrackChanged += track =>
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var item in AudioItems)
                    {
                        item.IsCurrent  = track != null && track.Id == item.Guide.Id;
                        item.IsPlaying  = item.IsCurrent && _audio.IsPlaying;
                    }
                });

            // Cập nhật khi pause/resume (track không đổi nhưng state đổi)
            _audio.OnPlaybackStateChanged += playing =>
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var item in AudioItems)
                        item.IsPlaying = item.IsCurrent && playing;
                });
        }

        public async Task LoadAsync(Restaurant restaurant)
        {
            Restaurant = restaurant;

            if (restaurant.DistanceMeters.HasValue)
                DistanceText = $"📍 {restaurant.DistanceMeters:F0}m";

            UpdateFavoriteUI();

            var guides = await App.Database.GetAudioGuidesAsync(restaurant.Id);
            AudioItems     = new ObservableCollection<AudioGuideItemVm>(
                                 guides.Select(g => new AudioGuideItemVm(g)));
            HasAudioGuides = AudioItems.Count > 0;

            // Khôi phục trạng thái nếu đang phát dở track nào đó
            if (_audio.CurrentTrack != null)
                foreach (var item in AudioItems)
                {
                    item.IsCurrent = _audio.CurrentTrack.Id == item.Guide.Id;
                    item.IsPlaying = item.IsCurrent && _audio.IsPlaying;
                }

            await App.Database.RecordVisitAsync(restaurant.Id);
        }

        [RelayCommand]
        async Task GoBack() => await Shell.Current.GoToAsync("..");

        [RelayCommand]
        async Task OpenMap() => await Shell.Current.GoToAsync("//Map");

        [RelayCommand]
        async Task PlayAudio(AudioGuideItemVm item)
        {
            if (item.IsCurrent && _audio.IsPlaying)
                _audio.Pause();                    // đang phát → pause
            else if (item.IsCurrent && !_audio.IsPlaying)
                _audio.Resume();                   // đang pause (cùng track) → resume
            else
                await _audio.PlayAsync(item.Guide); // track khác / chưa phát → play mới
        }

        [RelayCommand]
        async Task PlayFirstAudio()
        {
            var first = AudioItems.FirstOrDefault();
            if (first != null) await PlayAudio(first);
        }

        [RelayCommand]
        async Task ToggleFavorite()
        {
            if (Restaurant == null) return;
            await App.Database.ToggleFavoriteAsync(Restaurant.Id);
            Restaurant.IsFavorite = !Restaurant.IsFavorite;
            UpdateFavoriteUI();
        }

        private void UpdateFavoriteUI()
        {
            var isFav = Restaurant?.IsFavorite ?? false;
            FavoriteIcon  = isFav ? "icon_heart_filled.png" : "icon_heart.png";
            FavoriteLabel = isFav ? "❤️ Đã lưu" : "❤️ Thêm";
        }
    }
}
