using Microsoft.Extensions.Logging;
using Plugin.Maui.FileVault;

namespace Plugin.Maui.FileVault.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<MainPage>();

        builder
            .UseMauiApp<App>()
            .UseFileVault(options =>
            {
                options.DefaultTimeToLive = TimeSpan.FromMinutes(5);
                options.AutoPurgeOnResume = true;
                options.LockOnBackground = false;
                options.ExcludeFromBackup = true;
                options.SecureDelete = true;
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
