using Microsoft.Extensions.Logging;
using ProjectApp.Pages;

namespace ProjectApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Roboto-Regular.ttf",  "RobotoRegular");
                    fonts.AddFont("Roboto-Semibold.ttf", "RobotoSemibold");
                });

            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<AudioPage>();
            builder.Services.AddTransient<RestaurantDetailPage>();
            builder.Services.AddTransient<TourDetailPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
