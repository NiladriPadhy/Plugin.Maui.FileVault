namespace Plugin.Maui.FileVault;

/// <summary>
/// Snapshot of a file stored in the vault.
/// </summary>
public sealed class VaultFileInfo
{
    /// <summary>
    /// Logical path using <c>/</c> separators (for example <c>notes/secret.txt</c>).
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Stable identifier used as the on-disk file name.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Plaintext size in bytes.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Encrypted payload size in bytes.
    /// </summary>
    public long CipherSize { get; init; }

    /// <summary>
    /// Optional content type supplied at write time.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// UTC timestamp when the file was first written.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// UTC timestamp of the last content write.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; init; }

    /// <summary>
    /// UTC timestamp of the last successful read or write.
    /// </summary>
    public DateTimeOffset AccessedAt { get; init; }

    /// <summary>
    /// Absolute expiration, if any.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// When <c>true</c>, quota eviction will not delete this file.
    /// </summary>
    public bool IsPinned { get; init; }

    /// <summary>
    /// Caller-defined metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="utcNow"/> is at or after <see cref="ExpiresAt"/>.
    /// </summary>
    public bool IsExpired(DateTimeOffset utcNow) => ExpiresAt is { } expires && expires <= utcNow;
}
