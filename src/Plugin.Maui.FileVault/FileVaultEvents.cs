namespace Plugin.Maui.FileVault;

/// <summary>
/// Optional diagnostic callbacks configured on <see cref="FileVaultOptions"/>.
/// </summary>
public sealed class FileVaultEvents
{
    /// <summary>
    /// Invoked after a file is encrypted and persisted.
    /// </summary>
    public Action<VaultFileInfo>? OnWritten { get; set; }

    /// <summary>
    /// Invoked after a file is securely deleted.
    /// </summary>
    public Action<string>? OnDeleted { get; set; }

    /// <summary>
    /// Invoked after expired or idle files are purged.
    /// </summary>
    public Action<int>? OnPurged { get; set; }

    /// <summary>
    /// Invoked after the master key is cleared from memory.
    /// </summary>
    public Action? OnLocked { get; set; }

    /// <summary>
    /// Invoked after the vault becomes ready for file operations.
    /// </summary>
    public Action? OnUnlocked { get; set; }
}
