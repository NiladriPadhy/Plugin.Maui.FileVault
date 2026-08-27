namespace Plugin.Maui.FileVault;

/// <summary>
/// Raised after expired or idle files are removed.
/// </summary>
public sealed class VaultPurgedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes purge event arguments.
    /// </summary>
    public VaultPurgedEventArgs(int removedCount, IReadOnlyList<string> paths)
    {
        RemovedCount = removedCount;
        Paths = paths;
    }

    /// <summary>
    /// Number of files deleted.
    /// </summary>
    public int RemovedCount { get; }

    /// <summary>
    /// Logical paths that were deleted.
    /// </summary>
    public IReadOnlyList<string> Paths { get; }
}
