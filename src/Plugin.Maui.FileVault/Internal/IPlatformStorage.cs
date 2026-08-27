namespace Plugin.Maui.FileVault;

internal interface IPlatformStorage
{
    string ResolveRoot(string vaultName, bool excludeFromBackup, string? overrideRoot);

    void ProtectFile(string path);

    void ExcludeFromBackup(string path);
}
