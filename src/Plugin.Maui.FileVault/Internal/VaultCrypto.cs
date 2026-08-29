namespace Plugin.Maui.FileVault;

internal static class VaultCrypto
{
    public const int KeySize = 32;
    public const int NonceSize = 12;
    public const int TagSize = 16;
    public const byte Version = 1;

    public static ReadOnlySpan<byte> Magic => "FVLT"u8;

    public static byte[] GenerateKey() => RandomNumberGenerator.GetBytes(KeySize);

    public static byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData = default)
    {
        ValidateKey(key);

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using (var gcm = new AesGcm(key, TagSize))
        {
            gcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
        }

        var output = new byte[Magic.Length + 1 + NonceSize + ciphertext.Length + TagSize];
        Magic.CopyTo(output);
        output[Magic.Length] = Version;
        nonce.CopyTo(output.AsSpan(Magic.Length + 1));
        ciphertext.CopyTo(output.AsSpan(Magic.Length + 1 + NonceSize));
        tag.CopyTo(output.AsSpan(Magic.Length + 1 + NonceSize + ciphertext.Length));
        return output;
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData = default)
    {
        ValidateKey(key);

        var headerSize = Magic.Length + 1 + NonceSize + TagSize;
        if (payload.Length < headerSize)
        {
            throw new FileVaultException(FileVaultError.DecryptionFailed, "Encrypted payload is truncated.");
        }

        if (!payload[..Magic.Length].SequenceEqual(Magic))
        {
            throw new FileVaultException(FileVaultError.DecryptionFailed, "Encrypted payload has an unknown header.");
        }

        if (payload[Magic.Length] != Version)
        {
            throw new FileVaultException(FileVaultError.DecryptionFailed, $"Unsupported vault payload version {payload[Magic.Length]}.");
        }

        var nonce = payload.Slice(Magic.Length + 1, NonceSize);
        var cipherLength = payload.Length - headerSize;
        var ciphertext = payload.Slice(Magic.Length + 1 + NonceSize, cipherLength);
        var tag = payload.Slice(Magic.Length + 1 + NonceSize + cipherLength, TagSize);
        var plaintext = new byte[cipherLength];

        try
        {
            using var gcm = new AesGcm(key, TagSize);
            gcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        }
        catch (CryptographicException) when (!associatedData.IsEmpty)
        {
            // Legacy payloads written before file-id AAD binding.
            return Decrypt(payload, key);
        }
        catch (CryptographicException ex)
        {
            throw new FileVaultException(FileVaultError.DecryptionFailed, "The file could not be authenticated or decrypted.", ex);
        }

        return plaintext;
    }

    public static void Zero(byte[]? buffer)
    {
        if (buffer is { Length: > 0 })
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }

    static void ValidateKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySize)
        {
            throw new FileVaultException(FileVaultError.DecryptionFailed, "The vault master key is not 256 bits.");
        }
    }
}
