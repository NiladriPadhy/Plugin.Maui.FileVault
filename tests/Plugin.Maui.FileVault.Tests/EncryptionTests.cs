using System.Text;

namespace Plugin.Maui.FileVault.Tests;

public sealed class EncryptionTests
{
    [Fact]
    public void EncryptDecrypt_RoundTripsPayload()
    {
        var key = VaultCrypto.GenerateKey();
        var plain = "confidential vault bytes"u8.ToArray();

        var cipher = VaultCrypto.Encrypt(plain, key);
        var restored = VaultCrypto.Decrypt(cipher, key);

        Assert.Equal(plain, restored);
        Assert.False(cipher.AsSpan().StartsWith(plain));
    }

    [Fact]
    public void Encrypt_UsesUniqueNonce()
    {
        var key = VaultCrypto.GenerateKey();
        var plain = "same plaintext"u8.ToArray();

        var first = VaultCrypto.Encrypt(plain, key);
        var second = VaultCrypto.Encrypt(plain, key);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Decrypt_WrongKey_Fails()
    {
        var cipher = VaultCrypto.Encrypt("secret"u8.ToArray(), VaultCrypto.GenerateKey());

        var error = Assert.Throws<FileVaultException>(() => VaultCrypto.Decrypt(cipher, VaultCrypto.GenerateKey()));

        Assert.Equal(FileVaultError.DecryptionFailed, error.Error);
    }

    [Fact]
    public void Decrypt_TamperedPayload_Fails()
    {
        var key = VaultCrypto.GenerateKey();
        var cipher = VaultCrypto.Encrypt("secret"u8.ToArray(), key);
        cipher[^1] ^= 0xFF;

        var error = Assert.Throws<FileVaultException>(() => VaultCrypto.Decrypt(cipher, key));

        Assert.Equal(FileVaultError.DecryptionFailed, error.Error);
    }

    [Fact]
    public async Task WrittenFile_IsNotPlaintextOnDisk()
    {
        var root = Directory.CreateTempSubdirectory("filevault-disk-").FullName;
        using var vault = VaultHarness.Create(root: root);
        await vault.UnlockAsync();

        const string secret = "passport-number-99";
        await vault.WriteTextAsync("ids/passport.txt", secret);

        var files = Directory.GetFiles(Path.Combine(root, "test", "files"), "*.fv");
        Assert.Single(files);
        var disk = await File.ReadAllBytesAsync(files[0]);
        Assert.DoesNotContain(secret, Encoding.UTF8.GetString(disk));
    }
}
