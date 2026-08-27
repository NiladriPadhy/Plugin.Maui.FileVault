namespace Plugin.Maui.FileVault;

/// <summary>
/// Configuration for a <see cref="IFileVault"/> instance.
/// </summary>
public sealed class FileVaultOptions
{
    /// <summary>
    /// Folder name under the platform-protected storage root. Defaults to <see cref="FileVaultDefaults.VaultName"/>.
    /// </summary>
    public string VaultName { get; set; } = FileVaultDefaults.VaultName;

    /// <summary>
    /// Override the vault root. When set, files are stored under <c>{RootDirectory}/{VaultName}</c>.
    /// Intended for tests and custom locations.
    /// </summary>
    public string? RootDirectory { get; set; }

    /// <summary>
    /// When <c>true</c>, <see cref="IFileVault.UnlockAsync"/> requires a passphrase and
    /// the master key is not kept in <c>SecureStorage</c>.
    /// </summary>
    public bool RequirePassphrase { get; set; }

    /// <summary>
    /// Applied to writes that do not set <see cref="VaultWriteOptions.ExpiresAt"/> or
    /// <see cref="VaultWriteOptions.TimeToLive"/>.
    /// </summary>
    public TimeSpan? DefaultTimeToLive { get; set; }

    /// <summary>
    /// Files that have not been read or written within this interval are treated as expired.
    /// </summary>
    public TimeSpan? MaxIdleTime { get; set; }

    /// <summary>
    /// Maximum sum of ciphertext sizes. <c>null</c> means unlimited.
    /// </summary>
    public long? MaxVaultSizeBytes { get; set; }

    /// <summary>
    /// Maximum plaintext size for a single write.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = FileVaultDefaults.MaxFileSizeBytes;

    /// <summary>
    /// How to free space when <see cref="MaxVaultSizeBytes"/> is exceeded.
    /// </summary>
    public VaultEvictionPolicy EvictionPolicy { get; set; } = VaultEvictionPolicy.LeastRecentlyUsed;

    /// <summary>
    /// When <c>true</c>, expired and idle files are removed on unlock and when the app resumes.
    /// </summary>
    public bool AutoPurgeOnResume { get; set; } = true;

    /// <summary>
    /// Minimum time between automatic purges.
    /// </summary>
    public TimeSpan AutoPurgeInterval { get; set; } = FileVaultDefaults.AutoPurgeInterval;

    /// <summary>
    /// When <c>true</c>, <see cref="IFileVault.NotifyBackground"/> locks the vault.
    /// Passphrase vaults stay locked until the user unlocks again.
    /// Device-key vaults unlock automatically on the next operation or resume.
    /// </summary>
    public bool LockOnBackground { get; set; }

    /// <summary>
    /// Exclude the vault directory from iCloud / Android Auto Backup.
    /// </summary>
    public bool ExcludeFromBackup { get; set; } = true;

    /// <summary>
    /// Overwrite file contents with zeros before deleting.
    /// </summary>
    public bool SecureDelete { get; set; } = true;

    /// <summary>
    /// PBKDF2 iterations used when wrapping the master key with a passphrase.
    /// </summary>
    public int Pbkdf2Iterations { get; set; } = FileVaultDefaults.Pbkdf2Iterations;

    /// <summary>
    /// Diagnostic callbacks.
    /// </summary>
    public FileVaultEvents Events { get; set; } = new();
}
