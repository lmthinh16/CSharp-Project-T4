using ProjectApp.Services;

namespace ProjectApp.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly ApiService  _api  = App.Api;
        private readonly SyncService _sync = App.Sync;

        public LoginPage() => InitializeComponent();

        // ── Đăng nhập bằng tài khoản ─────────────────────────────
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var username = EntryUsername.Text?.Trim() ?? "";
            var password = EntryPassword.Text ?? "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập tài khoản và mật khẩu.");
                return;
            }

            SetLoading(true, "Đang đăng nhập...");

            var result = await _api.LoginWithDetailsAsync(username, password);

            if (result.Success)
            {
                UserSession.Current.LoginAsUser(
                    result.UserId, result.Username, result.FullName, result.Role);

                SetLoading(true, "Đang tải dữ liệu...");
                await _sync.SyncAllAsync();

                await NavigateToMain();
            }
            else
            {
                SetLoading(false);
                ShowError(result.Message);
            }
        }

        // ── Vào với tư cách khách (không cần tài khoản) ──────────
        private async void OnGuestClicked(object sender, EventArgs e)
        {
            SetLoading(true, "Đang tải dữ liệu...");

            UserSession.Current.LoginAsGuest();
            await _sync.SyncAllAsync();

            await NavigateToMain();
        }

        // ── Navigate sang AppShell (reset stack) ─────────────────
        private async Task NavigateToMain()
        {
            SetLoading(false);
            Application.Current!.MainPage = new AppShell();
            await Task.CompletedTask;
        }

        // ── UI helpers ────────────────────────────────────────────
        private void ShowError(string msg)
        {
            LblError.Text      = msg;
            LblError.IsVisible = true;
        }

        private void SetLoading(bool loading, string message = "")
        {
            LoadingOverlay.IsVisible = loading;
            LblLoading.Text          = message;
            LblError.IsVisible       = false;
            BtnLogin.IsEnabled       = !loading;
            BtnGuest.IsEnabled       = !loading;
        }
    }
}
