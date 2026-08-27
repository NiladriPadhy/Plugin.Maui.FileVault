namespace Plugin.Maui.FileVault.Tests;

public sealed class PassphraseTests
{
    [Fact]
    public async Task PassphraseVault_UnlocksWithCorrectSecret()
    {
        using var vault = VaultHarness.Create(options => options.RequirePassphrase = true);
        await vault.UnlockAsync("correct-horse");

        await vault.WriteTextAsync("safe.txt", "locked away");
        await vault.LockAsync();

        var locked = await Assert.ThrowsAsync<FileVaultException>(() => vault.ReadTextAsync("safe.txt"));
        Assert.Equal(FileVaultError.Locked, locked.Error);

        await vault.UnlockAsync("correct-horse");
        Assert.Equal("locked away", await vault.ReadTextAsync("safe.txt"));
    }

    [Fact]
    public async Task WrongPassphrase_IsRejected()
    {
        var root = Directory.CreateTempSubdirectory("filevault-pass-").FullName;
        var keys = new MemoryKeyStorage();

        using (var created = VaultHarness.Create(options => options.RequirePassphrase = true, keys: keys, root: root))
        {
            await created.UnlockAsync("alpha");
            await created.WriteTextAsync("a.txt", "a");
        }

        using var vault = VaultHarness.Create(options => options.RequirePassphrase = true, keys: keys, root: root);
        var error = await Assert.ThrowsAsync<FileVaultException>(() => vault.UnlockAsync("beta"));

        Assert.Equal(FileVaultError.InvalidPassphrase, error.Error);
        Assert.Equal(VaultState.Locked, vault.State);
    }

    [Fact]
    public async Task ChangePassphrase_KeepsFilesReadable()
    {
        using var vault = VaultHarness.Create(options => options.RequirePassphrase = true);
        await vault.UnlockAsync("old-secret");
        await vault.WriteTextAsync("keep.txt", "same key");

        await vault.ChangePassphraseAsync("old-secret", "new-secret");
        await vault.LockAsync();
        await vault.UnlockAsync("new-secret");

        Assert.Equal("same key", await vault.ReadTextAsync("keep.txt"));
    }

    [Fact]
    public async Task RemovePassphrase_AllowsDeviceUnlock()
    {
        using var vault = VaultHarness.Create(options => options.RequirePassphrase = true);
        await vault.UnlockAsync("temp-secret");
        await vault.WriteTextAsync("keep.txt", "still there");

        await vault.ChangePassphraseAsync("temp-secret", newPassphrase: null);
        await vault.LockAsync();
        await vault.UnlockAsync();

        Assert.Equal("still there", await vault.ReadTextAsync("keep.txt"));
    }
}
