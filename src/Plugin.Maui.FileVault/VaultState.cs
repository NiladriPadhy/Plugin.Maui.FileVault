namespace Plugin.Maui.FileVault;

/// <summary>
/// Lifecycle state of a file vault instance.
/// </summary>
public enum VaultState
{
    /// <summary>
    /// The vault has not been unlocked. Encrypted files cannot be read or written.
    /// </summary>
    Locked = 0,

    /// <summary>
    /// The master key is in memory and file operations are allowed.
    /// </summary>
    Unlocked = 1,

    /// <summary>
    /// The vault directory and keys have been wiped. A new unlock creates a fresh vault.
    /// </summary>
    Destroyed = 2
}
