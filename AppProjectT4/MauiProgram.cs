using Microsoft.Extensions.Logging;
using ProjectApp;

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
                    fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
                    fonts.AddFont("Roboto-Semibold.ttf", "RobotoSemibold");
                });
              



#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}