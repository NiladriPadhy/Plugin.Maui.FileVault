namespace Plugin.Maui.FileVault;

internal sealed class ManifestDocument
{
    public int Version { get; set; } = 1;

    public List<ManifestEntry> Files { get; set; } = [];
}

internal sealed class ManifestEntry
{
    public string Id { get; set; } = "";

    public string Path { get; set; } = "";

    public long Size { get; set; }

    public long CipherSize { get; set; }

    public string? ContentType { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public DateTimeOffset AccessedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsPinned { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.Ordinal);

    public VaultFileInfo ToInfo() => new()
    {
        Id = Id,
        Path = Path,
        Size = Size,
        CipherSize = CipherSize,
        ContentType = ContentType,
        CreatedAt = CreatedAt,
        ModifiedAt = ModifiedAt,
        AccessedAt = AccessedAt,
        ExpiresAt = ExpiresAt,
        IsPinned = IsPinned,
        Metadata = new Dictionary<string, string>(Metadata, StringComparer.Ordinal)
    };

    public ManifestEntry Clone() => new()
    {
        Id = Id,
        Path = Path,
        Size = Size,
        CipherSize = CipherSize,
        ContentType = ContentType,
        CreatedAt = CreatedAt,
        ModifiedAt = ModifiedAt,
        AccessedAt = AccessedAt,
        ExpiresAt = ExpiresAt,
        IsPinned = IsPinned,
        Metadata = new Dictionary<string, string>(Metadata, StringComparer.Ordinal)
    };
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false)]
[JsonSerializable(typeof(ManifestDocument))]
internal partial class ManifestJsonContext : JsonSerializerContext;
