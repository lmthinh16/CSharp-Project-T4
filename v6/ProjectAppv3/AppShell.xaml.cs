using ProjectApp.Pages;
using ProjectApp.Services;

namespace ProjectApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("RestaurantDetail", typeof(RestaurantDetailPage));
            Routing.RegisterRoute("TourDetail",       typeof(TourDetailPage));
            Routing.RegisterRoute("FavoritePage",     typeof(FavoritePage));

            UpdateTabTitles();
            LocalizationManager.Instance.PropertyChanged += (_, _) => UpdateTabTitles();
        }

        private void UpdateTabTitles()
        {
            var L = LocalizationManager.Instance;
            TabHome.Title    = L["tab_home"];
            TabMap.Title     = L["tab_map"];
            TabProfile.Title = L["tab_profile"];
        }
    }
}
