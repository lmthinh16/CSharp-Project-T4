using ProjectApp.Pages;
using ProjectApp.Services;

namespace ProjectApp
{
    public partial class App : Application
    {
        // ── Singletons truy cập toàn app ─────────────────────────
        public static DatabaseService Database { get; private set; } = null!;
        public static IAudioService AudioQueue { get; private set; } = null!;
        public static IAudioService Audio => AudioQueue;
        public static ApiService Api { get; private set; } = null!;
        public static SyncService Sync { get; private set; } = null!;
        public static GeofencingService Geofencing { get; private set; } = null!;
        public static Services.OfflineService Offline => Services.OfflineService.Instance;

        // ── Heartbeat timer ───────────────────────────────────────
        private static System.Timers.Timer? _heartbeatTimer;

        public App()
        {
            InitializeComponent();

            _ = Services.OfflineService.Instance;

            try
            {
                Database = new DatabaseService();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB Error: {ex.Message}");
                MainPage = new ContentPage
                {
                    Content = new Label
                    {
                        Text = "Lỗi khởi tạo database: " + ex.Message,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                };
                return;
            }

            AudioQueue   = new AudioPlayerService();
            Api          = new ApiService();
            Sync         = new SyncService(Api, Database);
            Geofencing   = new GeofencingService();

            // ✅ Heartbeat: ping server mỗi 2 phút để duy trì trạng thái online
            StartHeartbeatTimer();

            // ── Quyết định màn hình khởi động ────────────────────
            //
            // Ưu tiên 1: Session còn hợp lệ (user đã đăng nhập trước đó)
            //   → vào AppShell thẳng, sync ngầm
            //
            // Ưu tiên 2: Onboarding chưa xem
            //   → WelcomePage (3 slide giới thiệu)
            //   → WelcomePage.FinishAsync() sẽ set onboarding_done = true
            //      rồi navigate sang AppShell
            //
            // Ưu tiên 3: Đã xem onboarding, chưa đăng nhập
            //   → LoginPage

            // Sync chạy ngay lúc khởi động bất kể login hay chưa
            // Đảm bảo data từ CMS luôn được cập nhật dù user ở màn hình nào
            _ = Sync.SyncAllAsync();

            // Khởi tạo ngôn ngữ đã lưu trước khi render bất kỳ page nào
            Services.LocalizationManager.Instance.SetLanguage(UserSession.Language);

            if (!Preferences.Get("lang_selected", false))
            {
                // Lần đầu mở app → chọn ngôn ngữ trước
                MainPage = new NavigationPage(new Pages.LanguageSelectionPage(isOnboarding: true))
                {
                    BarBackgroundColor = Color.FromArgb("#0A1628"),
                    BarTextColor = Colors.White
                };
            }
            else if (UserSession.Current.IsLoggedIn)
            {
                MainPage = new AppShell();
            }
            else if (!Preferences.Get("onboarding_done", false))
            {
                MainPage = new NavigationPage(new WelcomePage())
                {
                    BarBackgroundColor = Color.FromArgb("#0A1628"),
                    BarTextColor = Colors.White
                };
            }
            else
            {
                MainPage = new NavigationPage(new LoginPage())
                {
                    BarBackgroundColor = Color.FromArgb("#080D14"),
                    BarTextColor = Colors.White
                };
            }
        }

        /// <summary>
        /// Khởi động timer ping heartbeat mỗi 2 phút.
        /// Cập nhật LastActiveAt trên server → web biết user đang online.
        /// </summary>
        // ── App lifecycle ─────────────────────────────────────────────
        protected override void OnSleep()
        {
            base.OnSleep();
            _heartbeatTimer?.Stop();
            var userId = UserSession.Current.IsAuthenticatedUser
                ? UserSession.Current.UserId : 0;
            // Block tối đa 1.5s để đảm bảo offline signal gửi xong trước khi Android kill process.
            // Task.Run tránh deadlock trên SynchronizationContext của main thread.
            Task.Run(() => Api.SendOfflineAsync(userId)).Wait(TimeSpan.FromMilliseconds(1500));
        }

        protected override void OnResume()
        {
            base.OnResume();
            _ = Task.Run(async () =>
            {
                var userId = UserSession.Current.IsAuthenticatedUser
                    ? UserSession.Current.UserId : 0;
                await Api.SendHeartbeatAsync(userId);
            });
            _heartbeatTimer?.Start();
        }

        private static void StartHeartbeatTimer()
        {
            // ✅ Ping ngay lập tức khi mở app — không chờ 2 phút mới tính online
            _ = Task.Run(async () =>
            {
                var userId = UserSession.Current.IsAuthenticatedUser
                    ? UserSession.Current.UserId
                    : 0;
                await Api.SendHeartbeatAsync(userId);
            });

            // Sau đó tiếp tục ping định kỳ mỗi 2 phút
            _heartbeatTimer = new System.Timers.Timer(TimeSpan.FromMinutes(2).TotalMilliseconds);
            _heartbeatTimer.Elapsed += async (_, _) =>
            {
                var userId = UserSession.Current.IsAuthenticatedUser
                    ? UserSession.Current.UserId
                    : 0;
                await Api.SendHeartbeatAsync(userId);
            };
            _heartbeatTimer.AutoReset = true;
            _heartbeatTimer.Start();
        }
    }
}
