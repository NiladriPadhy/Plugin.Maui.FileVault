namespace Plugin.Maui.FileVault;

/// <summary>
/// Per-write metadata and lifetime settings.
/// </summary>
public sealed class VaultWriteOptions
{
    /// <summary>
    /// Optional content type stored alongside the file (for example <c>text/plain</c>).
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Absolute expiration. Takes precedence over <see cref="TimeToLive"/>.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Lifetime measured from the write timestamp. Overrides <see cref="FileVaultOptions.DefaultTimeToLive"/>.
    /// </summary>
    public TimeSpan? TimeToLive { get; set; }

    /// <summary>
    /// When <c>true</c>, quota eviction will not delete this file.
    /// </summary>
    public bool Pin { get; set; }

    /// <summary>
    /// Caller-defined string metadata persisted in the encrypted manifest.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; set; }
}
