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
    //  RestaurantDetailViewModel  –  RestaurantDetailPage
    // ════════════════════════════════════════════════════════════
    public partial class RestaurantDetailViewModel : ObservableObject
    {
        private readonly IAudioService _audio = App.Audio;

        [ObservableProperty] Restaurant? _restaurant;
        [ObservableProperty] List<AudioGuide> _audioGuides = [];
        [ObservableProperty] bool   _hasAudioGuides;
        [ObservableProperty] string _favoriteIcon  = "icon_heart.png";
        [ObservableProperty] string _favoriteLabel = "❤️ Thêm";
        [ObservableProperty] string _distanceText  = "";

        public async Task LoadAsync(Restaurant restaurant)
        {
            Restaurant = restaurant;

            // Distance text
            if (restaurant.DistanceMeters.HasValue)
                DistanceText = $"📍 {restaurant.DistanceMeters:F0}m";

            // Favorite state
            UpdateFavoriteUI();

            // Audio guides
            var guides = await App.Database.GetAudioGuidesAsync(restaurant.Id);
            AudioGuides    = guides;
            HasAudioGuides = guides.Count > 0;

            // Record visit
            await App.Database.RecordVisitAsync(restaurant.Id);
        }

        [RelayCommand]
        async Task GoBack() => await Shell.Current.GoToAsync("..");

        [RelayCommand]
        async Task OpenMap() => await Shell.Current.GoToAsync("//Map");

        // Phát audio guide
        [RelayCommand]
        async Task PlayAudio(AudioGuide guide)
        {
            await _audio.PlayAsync(guide);
        }

        // Phát audio đầu tiên trong list
        [RelayCommand]
        void PlayFirstAudio()
        {
            if (AudioGuides.Count > 0)
                PlayAudio(AudioGuides[0]);
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
