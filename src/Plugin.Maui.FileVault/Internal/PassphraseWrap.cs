using System.Buffers.Binary;

namespace Plugin.Maui.FileVault;

internal static class PassphraseWrap
{
    public const int SaltSize = 16;
    public const byte Version = 1;

    public static ReadOnlySpan<byte> Magic => "FVK1"u8;

    public static byte[] Wrap(ReadOnlySpan<byte> masterKey, string passphrase, int iterations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passphrase);
        if (iterations < 100_000)
        {
            throw new FileVaultException(FileVaultError.InvalidOperation, "PBKDF2 iterations must be at least 100000.");
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var kek = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, iterations, HashAlgorithmName.SHA256, VaultCrypto.KeySize);
        try
        {
            var wrapped = VaultCrypto.Encrypt(masterKey, kek);
            var output = new byte[Magic.Length + 1 + sizeof(int) + SaltSize + wrapped.Length];
            Magic.CopyTo(output);
            output[Magic.Length] = Version;
            BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(Magic.Length + 1, sizeof(int)), iterations);
            salt.CopyTo(output.AsSpan(Magic.Length + 1 + sizeof(int)));
            wrapped.CopyTo(output.AsSpan(Magic.Length + 1 + sizeof(int) + SaltSize));
            return output;
        }
        finally
        {
            VaultCrypto.Zero(kek);
        }
    }

    public static byte[] Unwrap(ReadOnlySpan<byte> payload, string passphrase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passphrase);

        var headerSize = Magic.Length + 1 + sizeof(int) + SaltSize;
        if (payload.Length < headerSize + VaultCrypto.NonceSize + VaultCrypto.TagSize + 1)
        {
            throw new FileVaultException(FileVaultError.InvalidPassphrase, "The passphrase wrap file is truncated.");
        }

        if (!payload[..Magic.Length].SequenceEqual(Magic) || payload[Magic.Length] != Version)
        {
            throw new FileVaultException(FileVaultError.InvalidPassphrase, "The passphrase wrap file is not recognized.");
        }

        var iterations = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(Magic.Length + 1, sizeof(int)));
        if (iterations < 100_000)
        {
            throw new FileVaultException(FileVaultError.InvalidPassphrase, "The passphrase wrap file is not recognized.");
        }

        var salt = payload.Slice(Magic.Length + 1 + sizeof(int), SaltSize);
        var wrapped = payload[headerSize..];
        var kek = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, iterations, HashAlgorithmName.SHA256, VaultCrypto.KeySize);
        try
        {
            return VaultCrypto.Decrypt(wrapped, kek);
        }
        catch (FileVaultException ex) when (ex.Error == FileVaultError.DecryptionFailed)
        {
            throw new FileVaultException(FileVaultError.InvalidPassphrase, "The passphrase is incorrect.", ex);
        }
        finally
        {
            VaultCrypto.Zero(kek);
        }
    }
}
