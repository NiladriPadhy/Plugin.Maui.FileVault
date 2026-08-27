namespace Plugin.Maui.FileVault;

/// <summary>
/// Registers FileVault services without MAUI lifecycle hooks.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IFileVault"/> using the supplied options instance.
    /// </summary>
    public static IServiceCollection AddFileVault(this IServiceCollection services, FileVaultOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.TryAddSingleton<IFileVault>(sp =>
        {
            var vault = FileVault.Create(options);
            FileVault.SetDefault(vault);
            return vault;
        });

        return services;
    }

    /// <summary>
    /// Adds <see cref="IFileVault"/> and applies <paramref name="configure"/> to a new options instance.
    /// </summary>
    public static IServiceCollection AddFileVault(this IServiceCollection services, Action<FileVaultOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new FileVaultOptions();
        configure?.Invoke(options);
        return services.AddFileVault(options);
    }
}
