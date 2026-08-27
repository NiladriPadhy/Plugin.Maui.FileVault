namespace Plugin.Maui.FileVault;

/// <summary>
/// Point-in-time totals for an unlocked vault.
/// </summary>
public sealed class VaultStatistics
{
    /// <summary>
    /// Number of files currently tracked in the manifest.
    /// </summary>
    public int FileCount { get; init; }

    /// <summary>
    /// Sum of plaintext sizes.
    /// </summary>
    public long PlaintextBytes { get; init; }

    /// <summary>
    /// Sum of ciphertext sizes.
    /// </summary>
    public long CiphertextBytes { get; init; }

    /// <summary>
    /// Files that are expired or idle as of the snapshot time.
    /// </summary>
    public int ExpiredCount { get; init; }

    /// <summary>
    /// Oldest <see cref="VaultFileInfo.CreatedAt"/>, if the vault is not empty.
    /// </summary>
    public DateTimeOffset? OldestCreatedAt { get; init; }

    /// <summary>
    /// Newest <see cref="VaultFileInfo.ModifiedAt"/>, if the vault is not empty.
    /// </summary>
    public DateTimeOffset? NewestModifiedAt { get; init; }
}
