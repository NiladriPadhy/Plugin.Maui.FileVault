namespace Plugin.Maui.FileVault;

internal interface ISecureKeyStorage
{
    Task<string?> GetAsync(string key);

    Task SetAsync(string key, string value);

    bool Remove(string key);
}
