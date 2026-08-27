namespace Plugin.Maui.FileVault;

internal interface IClock
{
    DateTimeOffset UtcNow { get; }
}
