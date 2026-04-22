using ProjectApp.Pages;
using ProjectApp.Services;

namespace ProjectApp
{
    public partial class App : Application
    {
        // ── Singletons truy cập toàn app ─────────────────────────
        public static DatabaseService Database { get; private set; } = null!;
        public static AudioQueueService AudioQueue { get; private set; } = null!;

        // Alias giữ tương thích với code cũ dùng App.Audio
        public static AudioQueueService Audio => AudioQueue;
        public static ApiService Api { get; private set; } = null!;
        public static SyncService Sync { get; private set; } = null!;

        public App()
        {
            InitializeComponent();

            // Khởi tạo services
            try
            { 
                Database = new DatabaseService();
            }
            catch (Exception ex)
            {
                // Log lỗi ra debug
                System.Diagnostics.Debug.WriteLine($"DB Error: {ex.Message}");

                // Optional: hiển thị alert
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
            AudioQueue = new AudioQueueService();
            Api        = new ApiService();
            Sync       = new SyncService(Api, Database);

            // Kiểm tra session còn tồn tại không
            if (UserSession.Current.IsLoggedIn)
            {
                // Session còn → vào thẳng app, sync ngầm
                MainPage = new AppShell();
                _ = Sync.SyncAllAsync();
            }
            else
            {
                // Chưa đăng nhập → LoginPage
                MainPage = new NavigationPage(new LoginPage())
                {
                    BarBackgroundColor = Color.FromArgb("#0D1A2D"),
                    BarTextColor       = Colors.White
                };
            }
        }
    }
}
