using ProjectApp.Pages;

namespace ProjectApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("RestaurantDetail", typeof(RestaurantDetailPage));
            Routing.RegisterRoute("TourDetail",       typeof(TourDetailPage));
        }
    }
}
