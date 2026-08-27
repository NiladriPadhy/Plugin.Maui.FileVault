namespace Plugin.Maui.FileVault;

/// <summary>
/// Thrown when a vault operation cannot be completed.
/// </summary>
public sealed class FileVaultException : Exception
{
    /// <summary>
    /// Initializes a new exception with an error code and message.
    /// </summary>
    public FileVaultException(FileVaultError error, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Error = error;
    }

    /// <summary>
    /// Gets the classified error.
    /// </summary>
    public FileVaultError Error { get; }
}
