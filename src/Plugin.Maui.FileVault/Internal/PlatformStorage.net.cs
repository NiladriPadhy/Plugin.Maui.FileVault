#if !ANDROID && !IOS
using Microsoft.Maui.Storage;

namespace Plugin.Maui.FileVault;

sealed class PlatformStorage : IPlatformStorage
{
    public string ResolveRoot(string vaultName, bool excludeFromBackup, string? overrideRoot)
    {
        if (!string.IsNullOrWhiteSpace(overrideRoot))
        {
            var custom = VaultRoot.CombineOverride(overrideRoot, vaultName);
            Directory.CreateDirectory(custom);
            return custom;
        }

        string baseDir;
        try
        {
            baseDir = FileSystem.AppDataDirectory;
        }
        catch (Exception)
        {
            baseDir = Path.GetTempPath();
        }

        var root = Path.Combine(baseDir, "FileVault", vaultName);
        Directory.CreateDirectory(root);
        return root;
    }

    public void ProtectFile(string path)
    {
    }

    public void ExcludeFromBackup(string path)
    {
    }
}
#endif
