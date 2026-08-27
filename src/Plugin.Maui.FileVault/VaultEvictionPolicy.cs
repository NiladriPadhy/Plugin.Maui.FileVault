namespace Plugin.Maui.FileVault;

/// <summary>
/// How the vault frees space when <see cref="FileVaultOptions.MaxVaultSizeBytes"/> is exceeded.
/// </summary>
public enum VaultEvictionPolicy
{
    /// <summary>
    /// Do not delete other files. The write fails with <see cref="FileVaultError.QuotaExceeded"/>.
    /// </summary>
    None = 0,

    /// <summary>
    /// Delete unpinned files with the oldest <see cref="VaultFileInfo.AccessedAt"/> first.
    /// </summary>
    LeastRecentlyUsed = 1,

    /// <summary>
    /// Delete unpinned files with the oldest <see cref="VaultFileInfo.CreatedAt"/> first.
    /// </summary>
    Oldest = 2
}
