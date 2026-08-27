namespace Plugin.Maui.FileVault;

/// <summary>
/// Classifies a <see cref="FileVaultException"/>.
/// </summary>
public enum FileVaultError
{
    /// <summary>
    /// The operation is not valid in the current vault state.
    /// </summary>
    InvalidOperation = 0,

    /// <summary>
    /// The logical vault path is missing, rooted, or contains <c>..</c>.
    /// </summary>
    InvalidPath = 1,

    /// <summary>
    /// The vault is locked. Call <see cref="IFileVault.UnlockAsync"/> first.
    /// </summary>
    Locked = 2,

    /// <summary>
    /// A passphrase is required to unlock or create the vault.
    /// </summary>
    PassphraseRequired = 3,

    /// <summary>
    /// The supplied passphrase could not unwrap the master key.
    /// </summary>
    InvalidPassphrase = 4,

    /// <summary>
    /// No file exists at the requested path.
    /// </summary>
    NotFound = 5,

    /// <summary>
    /// The file has expired or exceeded its idle lifetime.
    /// </summary>
    Expired = 6,

    /// <summary>
    /// Ciphertext could not be authenticated or decrypted.
    /// </summary>
    DecryptionFailed = 7,

    /// <summary>
    /// The write would exceed the configured vault size quota.
    /// </summary>
    QuotaExceeded = 8,

    /// <summary>
    /// The plaintext is larger than <see cref="FileVaultOptions.MaxFileSizeBytes"/>.
    /// </summary>
    FileTooLarge = 9,

    /// <summary>
    /// The vault directory or a file could not be read or written.
    /// </summary>
    IoFailure = 10,

    /// <summary>
    /// The vault instance has been destroyed.
    /// </summary>
    Destroyed = 11
}
