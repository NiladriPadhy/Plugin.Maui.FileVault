namespace Plugin.Maui.FileVault;

static class VaultRoot
{
    public static string CombineOverride(string overrideRoot, string vaultName)
    {
        if (string.IsNullOrWhiteSpace(overrideRoot))
        {
            throw new FileVaultException(FileVaultError.InvalidPath, "RootDirectory cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(vaultName)
            || vaultName.Contains("..", StringComparison.Ordinal)
            || vaultName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new FileVaultException(FileVaultError.InvalidPath, "VaultName is not a valid folder name.");
        }

        var root = Path.GetFullPath(overrideRoot);
        var combined = Path.GetFullPath(Path.Combine(root, vaultName));
        var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                     + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(combined, Path.Combine(root, vaultName), StringComparison.OrdinalIgnoreCase))
        {
            throw new FileVaultException(
                FileVaultError.InvalidPath,
                "RootDirectory resolved outside the requested vault root.");
        }

        return combined;
    }
}
