using Microsoft.Extensions.Logging;
using ProjectApp.Pages;
using ZXing.Net.Maui.Controls;
#if ANDROID
using ProjectApp.Platforms.Android;
#endif
namespace ProjectApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()

                .UseBarcodeReader()

                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
                    fonts.AddFont("Roboto-Semibold.ttf", "RobotoSemibold");
                })

                // ✅ FIX: Dùng custom WebView handler trên Android thật
                // để bật JavaScript + cho phép load Leaflet CDN (unpkg.com)
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    handlers.AddHandler<Microsoft.Maui.Controls.WebView, CustomWebViewHandler>();
#endif
                });
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<AudioPage>();
            builder.Services.AddTransient<RestaurantDetailPage>();
            builder.Services.AddTransient<TourDetailPage>();
            builder.Services.AddTransient<QrEntryPage>();

            // Đăng ký các Services để MAUI DI có thể Inject vào constructor
            builder.Services.AddSingleton<Services.GeofencingService>();
            builder.Services.AddSingleton<Services.DatabaseService>(sp => App.Database);
            builder.Services.AddSingleton<Services.IAudioService>(sp => App.Audio);
            builder.Services.AddSingleton<Services.ApiService>(sp => App.Api);

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}