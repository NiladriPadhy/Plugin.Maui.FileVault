#if ANDROID
using AndroidApp = Android.App.Application;

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

        var context = AndroidApp.Context
            ?? throw new FileVaultException(FileVaultError.IoFailure, "The Android application context is not available.");

        var dir = excludeFromBackup ? context.NoBackupFilesDir : context.FilesDir;
        var root = Path.Combine(dir!.AbsolutePath, "FileVault", vaultName);
        Directory.CreateDirectory(root);

        try
        {
            var nomedia = Path.Combine(root, ".nomedia");
            if (!File.Exists(nomedia))
            {
                File.WriteAllBytes(nomedia, []);
            }
        }
        catch (IOException)
        {
            // Gallery hiding is best-effort.
        }

        return root;
    }

    public void ProtectFile(string path)
    {
        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception)
        {
            // App-private storage is already inaccessible to other apps.
        }
    }

    public void ExcludeFromBackup(string path)
    {
        // Files under NoBackupFilesDir are already excluded from Auto Backup.
    }
}
#endif
