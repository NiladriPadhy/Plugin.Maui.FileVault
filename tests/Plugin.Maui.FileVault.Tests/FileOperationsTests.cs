namespace Plugin.Maui.FileVault.Tests;

public sealed class FileOperationsTests
{
    [Fact]
    public async Task WriteAndReadText_RoundTrips()
    {
        using var vault = VaultHarness.Create();
        await vault.UnlockAsync();

        var info = await vault.WriteTextAsync("notes/hello.txt", "hello vault", new VaultWriteOptions
        {
            Metadata = new Dictionary<string, string> { ["owner"] = "qa" }
        });

        Assert.Equal("notes/hello.txt", info.Path);
        Assert.Equal("hello vault", await vault.ReadTextAsync(info.Path));
        Assert.Equal("qa", info.Metadata["owner"]);
        Assert.True(await vault.ExistsAsync(info.Path));
    }

    [Fact]
    public async Task Write_ReplacesExistingFile()
    {
        using var vault = VaultHarness.Create();
        await vault.UnlockAsync();

        var first = await vault.WriteTextAsync("note.txt", "v1");
        var second = await vault.WriteTextAsync("note.txt", "v2");

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("v2", await vault.ReadTextAsync("note.txt"));
        Assert.Equal(1, vault.GetStatistics().FileCount);
    }

    [Fact]
    public async Task List_FiltersDirectory()
    {
        using var vault = VaultHarness.Create();
        await vault.UnlockAsync();

        await vault.WriteTextAsync("notes/a.txt", "a");
        await vault.WriteTextAsync("notes/nested/b.txt", "b");
        await vault.WriteTextAsync("other/c.txt", "c");

        var recursive = await vault.ListAsync("notes", recursive: true);
        var direct = await vault.ListAsync("notes", recursive: false);

        Assert.Equal(2, recursive.Count);
        Assert.Single(direct);
        Assert.Equal("notes/a.txt", direct[0].Path);
    }

    [Fact]
    public async Task Delete_RemovesFile()
    {
        using var vault = VaultHarness.Create();
        await vault.UnlockAsync();
        await vault.WriteTextAsync("gone.txt", "bye");

        Assert.True(await vault.DeleteAsync("gone.txt"));
        Assert.False(await vault.ExistsAsync("gone.txt"));
        Assert.False(await vault.DeleteAsync("gone.txt"));
    }

    [Fact]
    public async Task Read_MissingFile_ThrowsNotFound()
    {
        using var vault = VaultHarness.Create();
        await vault.UnlockAsync();

        var error = await Assert.ThrowsAsync<FileVaultException>(() => vault.ReadTextAsync("missing.txt"));

        Assert.Equal(FileVaultError.NotFound, error.Error);
    }

    [Fact]
    public async Task Persistence_ReloadsManifest()
    {
        var root = Directory.CreateTempSubdirectory("filevault-persist-").FullName;
        var keys = new MemoryKeyStorage();

        using (var first = VaultHarness.Create(keys: keys, root: root))
        {
            await first.UnlockAsync();
            await first.WriteTextAsync("keep.txt", "still here");
        }

        using var second = VaultHarness.Create(keys: keys, root: root);
        await second.UnlockAsync();

        Assert.Equal("still here", await second.ReadTextAsync("keep.txt"));
    }

    [Fact]
    public async Task FileTooLarge_IsRejected()
    {
        using var vault = VaultHarness.Create(options => options.MaxFileSizeBytes = 8);
        await vault.UnlockAsync();

        var error = await Assert.ThrowsAsync<FileVaultException>(
            () => vault.WriteAsync("big.bin", new byte[16]));

        Assert.Equal(FileVaultError.FileTooLarge, error.Error);
    }
}
