using ProjectApp.Models;

namespace ProjectApp.Pages
{
    [QueryProperty(nameof(Tour), "Tour")]
    public partial class TourDetailPage : ContentPage
    {
        private Tour? _tour;

        public Tour? Tour
        {
            get => _tour;
            set { _tour = value; if (_tour != null) _ = LoadAsync(); }
        }

        public TourDetailPage() => InitializeComponent();

        private async Task LoadAsync()
        {
            if (_tour == null) return;

            TourTitleLabel.Text = $"{_tour.Emoji} {_tour.Name}";
            TourDescLabel.Text = _tour.Description;

            var all = await App.Database.GetRestaurantsAsync();
            var list = all.Where(r => _tour.RestaurantIds.Contains(r.Id)).ToList();

            TourMetaLabel.Text = $"⭐ {_tour.Rating} • {_tour.Duration} • {list.Count} địa điểm";
            RestaurantsCollection.ItemsSource = list;
        }

        private async void OnBackClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("..");
    }
}
