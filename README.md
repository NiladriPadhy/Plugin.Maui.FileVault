# Plugin.Maui.FileVault

Secure local files for **.NET MAUI** on **iOS** and **Android**.

The package stores app-private files as AES-256-GCM ciphertext, keeps the master key in the platform secure store, and manages the file lifecycle (expire, purge, lock, destroy).

| Feature | What it does |
| --- | --- |
| **Encryption** | AES-256-GCM per file, unique nonce, authenticated decrypt |
| **Key protection** | Master key in iOS Keychain / Android Keystore via `SecureStorage` |
| **Passphrase** | Optional PBKDF2-SHA256 wrap so the key never sits in `SecureStorage` |
| **Platform files** | iOS `NSFileProtectionComplete` + backup exclusion; Android app-private / no-backup dir |
| **Lifecycle** | TTL, idle timeout, purge on resume, lock on background, secure delete, quota eviction |

## Install

```bash
dotnet add package Plugin.Maui.FileVault
```

## Quick start

```csharp
using Plugin.Maui.FileVault;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseFileVault(options =>
            {
                options.DefaultTimeToLive = TimeSpan.FromDays(30);
                options.LockOnBackground = true;
                options.ExcludeFromBackup = true;
            });

        return builder.Build();
    }
}
```

Resolve `IFileVault` or use `FileVault.Current`:

```csharp
var vault = handler.Services.GetRequiredService<IFileVault>();

await vault.WriteTextAsync("notes/pin.txt", "1234", new VaultWriteOptions
{
    TimeToLive = TimeSpan.FromHours(12),
    Metadata = new Dictionary<string, string> { ["kind"] = "pin" }
});

var pin = await vault.ReadTextAsync("notes/pin.txt");
var files = await vault.ListAsync("notes");
```

Device-key vaults unlock automatically on first use. Passphrase vaults stay locked until `UnlockAsync`.

## Passphrase lock

```csharp
builder.UseFileVault(options => options.RequirePassphrase = true);

await vault.UnlockAsync(passphrase);
await vault.LockAsync();
await vault.ChangePassphraseAsync(current, next);
```

`ChangePassphraseAsync(current, newPassphrase: null)` moves the master key back to `SecureStorage`. Files are not re-encrypted; only the key wrap changes.

## Expiration and purge

```csharp
options.DefaultTimeToLive = TimeSpan.FromDays(7);
options.MaxIdleTime = TimeSpan.FromHours(6);
options.AutoPurgeOnResume = true;

await vault.SetExpirationAsync("cache/token.json", DateTimeOffset.UtcNow.AddMinutes(15));
var removed = await vault.PurgeExpiredAsync();
```

Reading an expired file deletes it and throws `FileVaultException` with `FileVaultError.Expired`.

## Quota

```csharp
options.MaxVaultSizeBytes = 10 * 1024 * 1024;
options.EvictionPolicy = VaultEvictionPolicy.LeastRecentlyUsed;

await vault.WriteAsync("photo.jpg", bytes, new VaultWriteOptions { Pin = true });
```

Pinned files are skipped during eviction. `VaultEvictionPolicy.None` fails the write with `QuotaExceeded`.

## Lifecycle hooks

`UseFileVault` wires platform resume/pause:

- **Resume** — purge expired and idle files
- **Background** — lock the vault when `LockOnBackground` is `true`

```csharp
vault.NotifyForeground();
vault.NotifyBackground();
```

`DestroyAsync` securely deletes every vault file, the manifest, and the stored key.

## Without the generic host

```csharp
var vault = FileVault.Create(new FileVaultOptions
{
    DefaultTimeToLive = TimeSpan.FromDays(1)
});

await vault.UnlockAsync();
```

## Target frameworks

The package targets `net10.0`, `net10.0-android`, and `net10.0-ios`.

## Pack from source

```bash
dotnet pack src/Plugin.Maui.FileVault/Plugin.Maui.FileVault.csproj -c Release -o artifacts
```

The `.nupkg` is written to `artifacts/Plugin.Maui.FileVault.1.0.0.nupkg`.

## License

MIT

## Support

> If this plugin saved you a weekend of native plumbing, consider buying me a coffee.
> Your support keeps it maintained, documented, and free.

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-ffdd00?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/npadhy)

This library stays open source. A coffee helps cover time for bug fixes, new features, and docs.
