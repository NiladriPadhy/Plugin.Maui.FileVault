namespace Plugin.Maui.FileVault;

/// <summary>
/// Raised after a file is written or deleted.
/// </summary>
public sealed class VaultFileEventArgs : EventArgs
{
    /// <summary>
    /// Initializes event arguments for a vault file.
    /// </summary>
    public VaultFileEventArgs(string path, VaultFileInfo? info)
    {
        Path = path;
        Info = info;
    }

    /// <summary>
    /// Logical path of the file.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// File metadata after a write, or <c>null</c> after a delete.
    /// </summary>
    public VaultFileInfo? Info { get; }
}
