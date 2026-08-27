namespace Plugin.Maui.FileVault.Tests;

sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 28, 6, 40, 0, TimeSpan.Zero);

    public void Advance(TimeSpan duration) => UtcNow += duration;
}

sealed class MemoryKeyStorage : ISecureKeyStorage
{
    readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public Task<string?> GetAsync(string key) =>
        Task.FromResult(_values.TryGetValue(key, out var value) ? value : null);

    public Task SetAsync(string key, string value)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }

    public bool Remove(string key) => _values.Remove(key);
}

sealed class TestPlatformStorage : IPlatformStorage
{
    public string ResolveRoot(string vaultName, bool excludeFromBackup, string? overrideRoot)
    {
        var root = Path.Combine(overrideRoot ?? Path.GetTempPath(), vaultName);
        Directory.CreateDirectory(root);
        return root;
    }

    public void ProtectFile(string path)
    {
    }

    public void ExcludeFromBackup(string path)
    {
    }
}

static class VaultHarness
{
    public static FileVaultImplementation Create(
        Action<FileVaultOptions>? configure = null,
        MemoryKeyStorage? keys = null,
        FakeClock? clock = null,
        string? root = null)
    {
        var directory = root ?? Directory.CreateTempSubdirectory("filevault-").FullName;
        var options = new FileVaultOptions
        {
            RootDirectory = directory,
            VaultName = "test",
            AutoPurgeOnResume = true,
            AutoPurgeInterval = TimeSpan.Zero,
            Pbkdf2Iterations = 100_000
        };
        configure?.Invoke(options);

        return FileVault.Create(
            options,
            keys ?? new MemoryKeyStorage(),
            new TestPlatformStorage(),
            clock ?? new FakeClock());
    }
}
