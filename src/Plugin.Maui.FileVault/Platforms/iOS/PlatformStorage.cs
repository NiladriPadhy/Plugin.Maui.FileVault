#if IOS
using Foundation;

namespace Plugin.Maui.FileVault;

sealed class PlatformStorage : IPlatformStorage
{
    public string ResolveRoot(string vaultName, bool excludeFromBackup, string? overrideRoot)
    {
        if (!string.IsNullOrWhiteSpace(overrideRoot))
        {
            var custom = Path.Combine(overrideRoot, vaultName);
            Directory.CreateDirectory(custom);
            return custom;
        }

        var support = NSSearchPath.GetDirectories(
            NSSearchPathDirectory.ApplicationSupportDirectory,
            NSSearchPathDomain.User,
            true).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(support))
        {
            support = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        var root = Path.Combine(support, "FileVault", vaultName);
        Directory.CreateDirectory(root);

        if (excludeFromBackup)
        {
            ExcludeFromBackup(root);
        }

        return root;
    }

    public void ProtectFile(string path)
    {
        NSFileManager.DefaultManager.SetAttributes(
            new NSFileAttributes { ProtectionKey = NSFileProtection.Complete },
            path,
            out _);
    }

    public void ExcludeFromBackup(string path)
    {
        try
        {
            var url = NSUrl.FromFilename(path);
            url.SetResource(NSUrl.IsExcludedFromBackupKey, NSNumber.FromBoolean(true), out _);
        }
        catch (Exception)
        {
            // Backup exclusion is best-effort.
        }
    }
}
#endif
