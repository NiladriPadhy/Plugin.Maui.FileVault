namespace Plugin.Maui.FileVault.Tests;

public sealed class LifecycleTests
{
    [Fact]
    public async Task ExpiredFile_IsPurged()
    {
        var clock = new FakeClock();
        using var vault = VaultHarness.Create(clock: clock);
        await vault.UnlockAsync();

        await vault.WriteTextAsync("temp.txt", "soon gone", new VaultWriteOptions
        {
            TimeToLive = TimeSpan.FromMinutes(10)
        });

        clock.Advance(TimeSpan.FromMinutes(11));
        var removed = await vault.PurgeExpiredAsync();

        Assert.Equal(1, removed);
        Assert.False(await vault.ExistsAsync("temp.txt"));
    }

    [Fact]
    public async Task Read_ExpiredFile_ThrowsAndDeletes()
    {
        var clock = new FakeClock();
        using var vault = VaultHarness.Create(clock: clock);
        await vault.UnlockAsync();
        await vault.WriteTextAsync("temp.txt", "gone", new VaultWriteOptions
        {
            TimeToLive = TimeSpan.FromMinutes(1)
        });

        clock.Advance(TimeSpan.FromMinutes(2));
        var error = await Assert.ThrowsAsync<FileVaultException>(() => vault.ReadTextAsync("temp.txt"));

        Assert.Equal(FileVaultError.Expired, error.Error);
        Assert.False(await vault.ExistsAsync("temp.txt"));
    }

    [Fact]
    public async Task IdleTimeout_ExpiresUnreadFile()
    {
        var clock = new FakeClock();
        using var vault = VaultHarness.Create(
            options => options.MaxIdleTime = TimeSpan.FromHours(2),
            clock: clock);
        await vault.UnlockAsync();
        await vault.WriteTextAsync("idle.txt", "waiting");

        clock.Advance(TimeSpan.FromHours(3));
        Assert.Equal(1, await vault.PurgeExpiredAsync());
    }

    [Fact]
    public async Task Touch_ExtendsIdleLifetime()
    {
        var clock = new FakeClock();
        using var vault = VaultHarness.Create(
            options => options.MaxIdleTime = TimeSpan.FromHours(2),
            clock: clock);
        await vault.UnlockAsync();
        await vault.WriteTextAsync("idle.txt", "waiting");

        clock.Advance(TimeSpan.FromHours(1));
        await vault.TouchAsync("idle.txt");
        clock.Advance(TimeSpan.FromHours(1.5));

        Assert.Equal(0, await vault.PurgeExpiredAsync());
        Assert.Equal("waiting", await vault.ReadTextAsync("idle.txt"));
    }

    [Fact]
    public async Task Quota_EvictsLeastRecentlyUsed()
    {
        using var vault = VaultHarness.Create(options =>
        {
            options.MaxFileSizeBytes = 1024;
            options.MaxVaultSizeBytes = 120;
            options.EvictionPolicy = VaultEvictionPolicy.LeastRecentlyUsed;
        });
        await vault.UnlockAsync();

        await vault.WriteAsync("old.bin", new byte[20]);
        await vault.WriteAsync("kept.bin", new byte[20]);
        await vault.ReadAsync("kept.bin");
        await vault.WriteAsync("new.bin", new byte[20]);

        Assert.False(await vault.ExistsAsync("old.bin"));
        Assert.True(await vault.ExistsAsync("kept.bin"));
        Assert.True(await vault.ExistsAsync("new.bin"));
    }

    [Fact]
    public async Task PinnedFile_IsNotEvicted()
    {
        using var vault = VaultHarness.Create(options =>
        {
            options.MaxVaultSizeBytes = 120;
            options.EvictionPolicy = VaultEvictionPolicy.LeastRecentlyUsed;
        });
        await vault.UnlockAsync();

        await vault.WriteAsync("pinned.bin", new byte[20], new VaultWriteOptions { Pin = true });
        await vault.WriteAsync("other.bin", new byte[20]);
        await vault.WriteAsync("third.bin", new byte[20]);

        Assert.True(await vault.ExistsAsync("pinned.bin"));
    }

    [Fact]
    public async Task Destroy_WipesVaultAndAllowsRecreate()
    {
        var keys = new MemoryKeyStorage();
        var root = Directory.CreateTempSubdirectory("filevault-destroy-").FullName;
        using var vault = VaultHarness.Create(keys: keys, root: root);
        await vault.UnlockAsync();
        await vault.WriteTextAsync("secret.txt", "wipe me");

        await vault.DestroyAsync();

        Assert.Equal(VaultState.Destroyed, vault.State);
        await vault.UnlockAsync();
        Assert.False(await vault.ExistsAsync("secret.txt"));
    }

    [Fact]
    public async Task LockOnBackground_RequiresUnlock()
    {
        using var vault = VaultHarness.Create(options => options.LockOnBackground = true);
        await vault.UnlockAsync();
        await vault.WriteTextAsync("a.txt", "a");

        vault.NotifyBackground();

        Assert.Equal(VaultState.Locked, vault.State);
    }
}
