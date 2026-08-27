namespace Plugin.Maui.FileVault;

/// <summary>
/// Default values used when <see cref="FileVaultOptions"/> does not override them.
/// </summary>
public static class FileVaultDefaults
{
    /// <summary>
    /// Default vault folder name under the platform-protected root.
    /// </summary>
    public const string VaultName = "default";

    /// <summary>
    /// PBKDF2 iteration count used to derive a wrapping key from a passphrase.
    /// </summary>
    public const int Pbkdf2Iterations = 210_000;

    /// <summary>
    /// Maximum plaintext size accepted by a single write (64 MiB).
    /// </summary>
    public const long MaxFileSizeBytes = 64L * 1024 * 1024;

    /// <summary>
    /// Minimum time between automatic expired-file purges.
    /// </summary>
    public static readonly TimeSpan AutoPurgeInterval = TimeSpan.FromMinutes(5);
}
