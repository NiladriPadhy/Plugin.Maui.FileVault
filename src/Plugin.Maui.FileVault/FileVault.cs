namespace Plugin.Maui.FileVault;

/// <summary>
/// Entry point for the FileVault plugin when dependency injection is not used.
/// </summary>
public static class FileVault
{
    static IFileVault? _current;

    /// <summary>
    /// Gets the shared <see cref="IFileVault"/> instance.
    /// </summary>
    public static IFileVault Current => _current ??= Create(new FileVaultOptions());

    /// <summary>
    /// Creates a vault using MAUI <c>SecureStorage</c> and platform-protected app storage.
    /// </summary>
    public static IFileVault Create(FileVaultOptions? options = null) =>
        new FileVaultImplementation(
            options ?? new FileVaultOptions(),
            new MauiSecureKeyStorage(),
            new PlatformStorage(),
            SystemClock.Instance);

    /// <summary>
    /// Replaces the shared instance. Intended for tests and custom implementations.
    /// </summary>
    public static void SetDefault(IFileVault implementation) =>
        _current = implementation ?? throw new ArgumentNullException(nameof(implementation));

    internal static FileVaultImplementation Create(
        FileVaultOptions options,
        ISecureKeyStorage keys,
        IPlatformStorage storage,
        IClock clock) =>
        new(options, keys, storage, clock);
}
