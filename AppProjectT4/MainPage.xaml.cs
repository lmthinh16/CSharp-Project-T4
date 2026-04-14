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
                await DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
        }

        private async void OnRestaurantSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Restaurant restaurant)
            {
                await Navigation.PushAsync(new RestaurantDetails(restaurant));

                ((CollectionView)sender).SelectedItem = null;
            }
        }
        private async void OnMapClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MapPage());
        }

    }
}

