using Microsoft.Maui.Storage;

namespace Plugin.Maui.FileVault;

sealed class MauiSecureKeyStorage : ISecureKeyStorage
{
    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);

    public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);

    public bool Remove(string key) => SecureStorage.Default.Remove(key);
}
