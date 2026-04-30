using ProjectApp.Models;
using ProjectApp.ViewModels;

namespace ProjectApp.Pages
{
    [QueryProperty(nameof(Restaurant), "Restaurant")]
    public partial class RestaurantDetailPage : ContentPage
    {
        private readonly RestaurantDetailViewModel _vm;

        public Restaurant? Restaurant
        {
            set { if (value != null) _ = _vm.LoadAsync(value); }
        }

        public RestaurantDetailPage()
        {
            InitializeComponent();
            BindingContext = _vm = new RestaurantDetailViewModel();
        }

        /// <summary>Constructor trực tiếp nhận Restaurant — dùng khi push từ MapPage.</summary>
        public RestaurantDetailPage(Restaurant restaurant) : this()
        {
            _ = _vm.LoadAsync(restaurant);
        }

        private async void OnBookingClicked(object sender, EventArgs e)
        {
            var restaurant = (BindingContext as ViewModels.RestaurantDetailViewModel)?.Restaurant;
            if (restaurant == null) return;
            await Navigation.PushAsync(new Pages.BookingPage(restaurant));
        }

    }
}
