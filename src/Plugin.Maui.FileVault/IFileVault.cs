namespace Plugin.Maui.FileVault;

/// <summary>
/// Encrypted local file store with expiration, purge, and lock/unlock lifecycle.
/// </summary>
public interface IFileVault
{
    /// <summary>
    /// Gets the current lifecycle state.
    /// </summary>
    VaultState State { get; }

    /// <summary>
    /// Gets a value indicating whether the master key is in memory.
    /// </summary>
    bool IsUnlocked { get; }

    /// <summary>
    /// Gets a value indicating whether this target can persist a vault.
    /// Always <c>true</c> for Android, iOS, and the shared <c>net10.0</c> surface.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Raised after a file is written.
    /// </summary>
    event EventHandler<VaultFileEventArgs>? FileWritten;

    /// <summary>
    /// Raised after a file is deleted.
    /// </summary>
    event EventHandler<VaultFileEventArgs>? FileDeleted;

    /// <summary>
    /// Raised after expired or idle files are purged.
    /// </summary>
    event EventHandler<VaultPurgedEventArgs>? ExpiredPurged;

    /// <summary>
    /// Raised after the vault is locked.
    /// </summary>
    event EventHandler? Locked;

    /// <summary>
    /// Raised after the vault is unlocked.
    /// </summary>
    event EventHandler? Unlocked;

    /// <summary>
    /// Loads the master key. A passphrase is required when the vault was created with one
    /// or when <see cref="FileVaultOptions.RequirePassphrase"/> is <c>true</c>.
    /// </summary>
    Task UnlockAsync(string? passphrase = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the master key from memory.
    /// </summary>
    Task LockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts <paramref name="content"/> and stores it at <paramref name="path"/>.
    /// Replaces an existing file at the same path.
    /// </summary>
    Task<VaultFileInfo> WriteAsync(string path, Stream content, VaultWriteOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts <paramref name="content"/> and stores it at <paramref name="path"/>.
    /// </summary>
    Task<VaultFileInfo> WriteAsync(string path, byte[] content, VaultWriteOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts UTF-8 text and stores it at <paramref name="path"/>.
    /// </summary>
    Task<VaultFileInfo> WriteTextAsync(string path, string text, VaultWriteOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts the file and returns a readable stream. The caller disposes the stream.
    /// </summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts the file into a byte array.
    /// </summary>
    Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts the file as UTF-8 text.
    /// </summary>
    Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> when a non-expired file exists at <paramref name="path"/>.
    /// </summary>
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns metadata for a file, or <c>null</c> when it is missing or expired.
    /// </summary>
    Task<VaultFileInfo?> GetInfoAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files, optionally limited to a logical directory.
    /// </summary>
    Task<IReadOnlyList<VaultFileInfo>> ListAsync(string? directory = null, bool recursive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Securely deletes a file. Returns <c>false</c> when it did not exist.
    /// </summary>
    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or clears absolute expiration for an existing file.
    /// </summary>
    Task<VaultFileInfo> SetExpirationAsync(string path, DateTimeOffset? expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates <see cref="VaultFileInfo.AccessedAt"/> without reading content.
    /// </summary>
    Task<VaultFileInfo> TouchAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired and idle files. Returns the number of files removed.
    /// </summary>
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-wraps the master key. Pass <paramref name="newPassphrase"/> as <c>null</c> or empty
    /// to store the key in the platform secure store instead.
    /// </summary>
    Task ChangePassphraseAsync(string? currentPassphrase, string? newPassphrase, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wipes every vault file, the manifest, and the stored master key.
    /// </summary>
    Task DestroyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns vault totals. The vault must be unlocked.
    /// Waits up to 5 seconds for an in-flight operation; prefer
    /// <see cref="GetStatisticsAsync"/> from UI or async code.
    /// </summary>
    VaultStatistics GetStatistics();

    /// <summary>
    /// Returns vault totals without blocking the caller on a sync semaphore wait.
    /// The vault must be unlocked.
    /// </summary>
    Task<VaultStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the app returns to the foreground. Purges expired files when configured.
    /// </summary>
    void NotifyForeground();

    /// <summary>
    /// Called when the app moves to the background. Locks the vault when configured.
    /// </summary>
    void NotifyBackground();
}
