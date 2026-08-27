namespace Plugin.Maui.FileVault.Tests;

public sealed class PathTests
{
    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("notes/../../etc/passwd")]
    [InlineData("notes/./hide.txt")]
    public async Task TraversalPaths_AreRejected(string path)
    {
        using var vault = VaultHarness.Create();
        await vault.UnlockAsync();

        var error = await Assert.ThrowsAsync<FileVaultException>(() => vault.WriteTextAsync(path, "nope"));

        Assert.Equal(FileVaultError.InvalidPath, error.Error);
    }

    [Fact]
    public async Task RootedPath_IsNormalized()
    {
        using var vault = VaultHarness.Create();
        await vault.UnlockAsync();

        var info = await vault.WriteTextAsync("/notes\\hello.txt", "ok");

        Assert.Equal("notes/hello.txt", info.Path);
        Assert.Equal("ok", await vault.ReadTextAsync("notes/hello.txt"));
    }

    [Fact]
    public void EmptyPath_IsRejected()
    {
        var error = Assert.Throws<FileVaultException>(() => VaultPath.Normalize("   "));
        Assert.Equal(FileVaultError.InvalidPath, error.Error);
    }
}
