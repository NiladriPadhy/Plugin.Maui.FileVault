using Plugin.Maui.FileVault;

namespace Plugin.Maui.FileVault.Sample;

public partial class MainPage : ContentPage
{
    readonly IFileVault _vault;

    public MainPage(IFileVault vault)
    {
        InitializeComponent();
        _vault = vault;
        _vault.FileWritten += (_, e) => MainThread.BeginInvokeOnMainThread(() =>
            StatusLabel.Text = $"Wrote {e.Path} ({e.Info?.Size} bytes plaintext).");
        _vault.ExpiredPurged += (_, e) => MainThread.BeginInvokeOnMainThread(() =>
            StatusLabel.Text = $"Purged {e.RemovedCount} file(s).");
    }

    async void OnWriteClicked(object? sender, EventArgs e)
        => await RunAsync("Write", async () =>
        {
            await EnsureUnlockedAsync();
            var info = await _vault.WriteTextAsync(PathEntry.Text, ContentEditor.Text ?? string.Empty);
            return $"{info.Path} stored. Cipher {info.CipherSize} bytes. Expires {Format(info.ExpiresAt)}.";
        });

    async void OnReadClicked(object? sender, EventArgs e)
        => await RunAsync("Read", async () =>
        {
            await EnsureUnlockedAsync();
            var text = await _vault.ReadTextAsync(PathEntry.Text);
            ContentEditor.Text = text;
            return text;
        });

    async void OnExpireClicked(object? sender, EventArgs e)
        => await RunAsync("Expire", async () =>
        {
            await EnsureUnlockedAsync();
            var info = await _vault.SetExpirationAsync(PathEntry.Text, DateTimeOffset.UtcNow.AddSeconds(10));
            return $"{info.Path} expires at {Format(info.ExpiresAt)}.";
        });

    async void OnListClicked(object? sender, EventArgs e)
        => await RunAsync("List", async () =>
        {
            await EnsureUnlockedAsync();
            var files = await _vault.ListAsync();
            var stats = _vault.GetStatistics();
            if (files.Count == 0)
            {
                return $"Empty vault. State: {_vault.State}.";
            }

            var lines = files.Select(f =>
                $"{f.Path}  {f.Size} B  exp {Format(f.ExpiresAt)}");
            return $"Files {stats.FileCount}, cipher {stats.CiphertextBytes} B{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
        });

    async void OnPurgeClicked(object? sender, EventArgs e)
        => await RunAsync("Purge", async () =>
        {
            await EnsureUnlockedAsync();
            var removed = await _vault.PurgeExpiredAsync();
            return $"Removed {removed} expired file(s).";
        });

    async void OnLockUnlockClicked(object? sender, EventArgs e)
        => await RunAsync("Lock/Unlock", async () =>
        {
            if (_vault.IsUnlocked)
            {
                await _vault.LockAsync();
                return "Vault locked. Master key cleared from memory.";
            }

            await EnsureUnlockedAsync();
            return "Vault unlocked.";
        });

    async void OnDestroyClicked(object? sender, EventArgs e)
        => await RunAsync("Destroy", async () =>
        {
            await _vault.DestroyAsync();
            return "Vault destroyed. The next write creates a new one.";
        });

    async Task EnsureUnlockedAsync()
    {
        if (_vault.IsUnlocked)
        {
            return;
        }

        var passphrase = string.IsNullOrWhiteSpace(PassphraseEntry.Text) ? null : PassphraseEntry.Text;
        await _vault.UnlockAsync(passphrase);
    }

    async Task RunAsync(string action, Func<Task<string>> work)
    {
        try
        {
            StatusLabel.Text = await work();
        }
        catch (FileVaultException ex)
        {
            StatusLabel.Text = $"{action} failed ({ex.Error}): {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"{action} failed: {ex.Message}";
        }
    }

    static string Format(DateTimeOffset? value) =>
        value is { } stamp ? stamp.ToLocalTime().ToString("HH:mm:ss") : "never";
}
