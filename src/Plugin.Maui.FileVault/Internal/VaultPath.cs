namespace Plugin.Maui.FileVault;

internal static class VaultPath
{
    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new FileVaultException(FileVaultError.InvalidPath, "A vault path is required.");
        }

        var trimmed = path.Replace('\\', '/').Trim();
        if (Path.IsPathRooted(trimmed) || trimmed.StartsWith('/'))
        {
            trimmed = trimmed.TrimStart('/', '\\');
        }

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new FileVaultException(FileVaultError.InvalidPath, "A vault path is required.");
        }

        var parts = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            throw new FileVaultException(FileVaultError.InvalidPath, "A vault path is required.");
        }

        foreach (var part in parts)
        {
            if (part is "." or "..")
            {
                throw new FileVaultException(FileVaultError.InvalidPath, "Vault paths cannot contain '.' or '..' segments.");
            }

            foreach (var ch in part)
            {
                if (char.IsControl(ch) || ch is ':' or '*' or '?' or '"' or '<' or '>' or '|')
                {
                    throw new FileVaultException(FileVaultError.InvalidPath, $"Vault path contains an invalid character in '{part}'.");
                }
            }
        }

        return string.Join('/', parts);
    }

    public static bool IsUnderDirectory(string path, string? directory, bool recursive)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return true;
        }

        var dir = Normalize(directory);
        if (path.Equals(dir, StringComparison.Ordinal))
        {
            return true;
        }

        if (!path.StartsWith(dir + "/", StringComparison.Ordinal))
        {
            return false;
        }

        if (recursive)
        {
            return true;
        }

        var remainder = path[(dir.Length + 1)..];
        return !remainder.Contains('/', StringComparison.Ordinal);
    }
}
