using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

namespace Plugin.Maui.FileVault;

/// <summary>
/// MAUI host registration for FileVault.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="IFileVault"/> as a singleton and wires Android/iOS lifecycle hooks
    /// for lock-on-background and purge-on-resume.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseFileVault(options =>
    /// {
    ///     options.DefaultTimeToLive = TimeSpan.FromDays(30);
    ///     options.LockOnBackground = true;
    /// });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseFileVault(this MauiAppBuilder builder, Action<FileVaultOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new FileVaultOptions();
        configure?.Invoke(options);

        builder.Services.AddFileVault(options);
        builder.Services.AddTransient<IMauiInitializeService, FileVaultInitializer>();

        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android =>
            {
                android.OnResume(_ => FileVault.Current.NotifyForeground());
                android.OnPause(_ => FileVault.Current.NotifyBackground());
            });
#elif IOS
            events.AddiOS(ios =>
            {
                ios.OnActivated(_ => FileVault.Current.NotifyForeground());
                ios.DidEnterBackground(_ => FileVault.Current.NotifyBackground());
            });
#endif
        });

        return builder;
    }
}
