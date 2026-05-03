using ProjectApp.Pages;
using ProjectApp.Services;

namespace ProjectApp
{
    public partial class ProfilePage : ContentPage
    {
        private readonly UserSession _session = UserSession.Current;

        public ProfilePage() => InitializeComponent();

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LoadSessionInfo();
            await LoadStatsAsync();
        }

        private void LoadSessionInfo()
        {
            LblDisplayName.Text = _session.DisplayName;
            LblUsername.Text    = _session.IsGuest
                ? "Khách ẩn danh"
                : $"@{_session.Username}";
            LblRole.Text = _session.Role switch
            {
                "admin" => "🛡️ Quản trị viên",
                "owner" => "🏪 Chủ quán",
                _       => _session.IsGuest ? "👤 Khách" : "👤 Người dùng"
            };
            LblInitial.Text = _session.AvatarInitial;
        }

        private async Task LoadStatsAsync()
        {
            try
            {
                var visitCount = await App.Database.GetVisitCountAsync();
                var tours      = await App.Database.GetToursAsync();
                var favorites  = await App.Database.GetFavoritesAsync();

                LblVisitCount.Text    = visitCount.ToString();
                LblTourCount.Text     = tours.Count.ToString();
                LblFavoriteCount.Text = favorites.Count.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Profile] {ex.Message}");
            }
        }

        private async void OnBookingHistoryClicked(object sender, EventArgs e)
                {
                    await Navigation.PushAsync(new Pages.BookingHistoryPage());
                }
        private async void OnVisitHistoryClicked(object sender, EventArgs e)
            => await DisplayAlert("Lịch sử ghé thăm", "Tính năng đang phát triển", "OK");

        private async void OnFavoritesClicked(object sender, EventArgs e)
            => await DisplayAlert("Quán yêu thích", "Tính năng đang phát triển", "OK");

        private async void OnAnalyticsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Pages.AnalyticsPage());
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Đăng xuất", "Bạn có chắc muốn đăng xuất?", "Đăng xuất", "Hủy");
            if (!confirm) return;

            UserSession.Current.Logout();
            Application.Current!.MainPage = new NavigationPage(new LoginPage())
            {
                BarBackgroundColor = Color.FromArgb("#0D1A2D"),
                BarTextColor       = Colors.White
            };
        }

    }
}
