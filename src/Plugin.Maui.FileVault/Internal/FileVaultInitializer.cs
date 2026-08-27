using Microsoft.Maui.Hosting;

namespace Plugin.Maui.FileVault;

sealed class FileVaultInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var vault = services.GetService<IFileVault>() ?? FileVault.Current;
        FileVault.SetDefault(vault);

        var options = services.GetService<FileVaultOptions>() ?? new FileVaultOptions();
        if (options.RequirePassphrase)
        {
            return;
        }

        _ = UnlockQuietlyAsync(vault);
    }

    static async Task UnlockQuietlyAsync(IFileVault vault)
    {
        try
        {
            await vault.UnlockAsync().ConfigureAwait(false);
        }
        catch (FileVaultException)
        {
            // Stay locked. The first file operation will surface the error.
        }
    }
}
