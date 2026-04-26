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
    }
}
