using ProjectApp.Models;

namespace ProjectApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRestaurantsAsync();
        }

        private async Task LoadRestaurantsAsync()
        {
            try
            {
                var restaurants = await App.Database.GetRestaurantsAsync();

                // If it's empty, wait 1 second and try again (in case App.xaml.cs is still saving)
                if (restaurants.Count == 0)
                {
                    await Task.Delay(1000);
                    restaurants = await App.Database.GetRestaurantsAsync();
                }

                RestaurantsCollection.ItemsSource = restaurants;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", ex.Message, "OK");
            }
        }

        private async void OnRestaurantSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Restaurant restaurant)
            {
                await DisplayAlert(
                    restaurant.Name,
                    $"{restaurant.Description}\n\n" +
                    $"📍 {restaurant.Address}\n" +
                    $"⭐ Rating: {restaurant.Rating}\n" +
                    $"🕐 {restaurant.OpenHours}\n\n" +
                    $"📌 Tọa độ: {restaurant.Latitude}, {restaurant.Longitude}",
                    "OK"
                );

                ((CollectionView)sender).SelectedItem = null;
            }
        }
        private async void OnGpsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GpsPage());
        }
        private async void OnMapClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MapPage());
        }

        private void OnTour1Clicked(object sender, EventArgs e)
        {
            var tour = new Tour
            {
                Id = "1",
                Name = "Tour Ăn Ốc",
                Description = "3 quán ốc ngon nổi tiếng",
                Emoji = "🦪",
                Duration = "45 phút",
                Rating = 4.4,
                RestaurantIds = new List<int> { 1, 2, 3 }
            };

            Navigation.PushAsync(new TourDetailPage(tour));
        }

        private void OnTour2Clicked(object sender, EventArgs e)
        {
            var tour = new Tour
            {
                Id = "2",
                Name = "Tour Ăn Nướng",
                Description = "Lẩu nướng, bò lá lốt",
                Emoji = "🔥",
                Duration = "60 phút",
                Rating = 4.5,
                RestaurantIds = new List<int> { 7, 8, 10 }
            };

            Navigation.PushAsync(new TourDetailPage(tour));
        }

        private void OnTour3Clicked(object sender, EventArgs e)
        {
            var tour = new Tour
            {
                Id = "3",
                Name = "Tour Ăn Vặt",
                Description = "Cơm cháy, bún thịt nướng",
                Emoji = "🍢",
                Duration = "40 phút",
                Rating = 4.3,
                RestaurantIds = new List<int> { 9, 11 }
            };

            Navigation.PushAsync(new TourDetailPage(tour));
        }

        private void OnTour4Clicked(object sender, EventArgs e)
        {
            var tour = new Tour
            {
                Id = "4",
                Name = "Tour Đặc Sản",
                Description = "Bún cá Châu Đốc",
                Emoji = "⭐",
                Duration = "50 phút",
                Rating = 4.6,
                RestaurantIds = new List<int> { 4, 5, 6 }
            };

            Navigation.PushAsync(new TourDetailPage(tour));
        }
    }
}

