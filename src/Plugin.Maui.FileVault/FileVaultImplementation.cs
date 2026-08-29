namespace Plugin.Maui.FileVault;

sealed class FileVaultImplementation : IFileVault, IDisposable
{
    const string ManifestFileName = "manifest.enc";
    const string WrapFileName = "key.wrap";
    const string FilesFolderName = "files";

    readonly FileVaultOptions _options;
    readonly ISecureKeyStorage _keys;
    readonly IPlatformStorage _storage;
    readonly IClock _clock;
    readonly SemaphoreSlim _gate = new(1, 1);
    readonly string _secureKeyName;

    string? _root;
    byte[]? _masterKey;
    ManifestDocument? _manifest;
    DateTimeOffset _lastAutoPurge = DateTimeOffset.MinValue;
    bool _disposed;
    bool _explicitlyLocked;

    public FileVaultImplementation(
        FileVaultOptions options,
        ISecureKeyStorage keys,
        IPlatformStorage storage,
        IClock clock)
    {
        _options = options;
        _keys = keys;
        _storage = storage;
        _clock = clock;
        _secureKeyName = $"plugin.maui.filevault.{SanitizeVaultName(options.VaultName)}.master";
    }

    public VaultState State { get; private set; } = VaultState.Locked;

    public bool IsUnlocked => State == VaultState.Unlocked && _masterKey is not null;

    public bool IsSupported => true;

    public event EventHandler<VaultFileEventArgs>? FileWritten;

    public event EventHandler<VaultFileEventArgs>? FileDeleted;

    public event EventHandler<VaultPurgedEventArgs>? ExpiredPurged;

    public event EventHandler? Locked;

    public event EventHandler? Unlocked;

    public async Task UnlockAsync(string? passphrase = null, CancellationToken cancellationToken = default)
    {
        List<string> purged = [];
        var becameUnlocked = false;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            var lockedWhenStarted = _explicitlyLocked;
            if (IsUnlocked)
            {
                _explicitlyLocked = false;
                return;
            }

            await UnlockUnlockedHeldAsync(passphrase, cancellationToken).ConfigureAwait(false);
            if (_explicitlyLocked && !lockedWhenStarted)
            {
                ClearKey();
                _manifest = null;
                State = VaultState.Locked;
                return;
            }

            _explicitlyLocked = false;
            purged = AutoPurgeIfDue(force: true);
            becameUnlocked = true;
        }
        catch
        {
            ClearKey();
            _manifest = null;
            if (State != VaultState.Destroyed)
            {
                State = VaultState.Locked;
            }

            throw;
        }
        finally
        {
            _gate.Release();
        }

        RaisePurged(purged);
        if (becameUnlocked)
        {
            RaiseUnlocked();
        }
    }

    public async Task LockAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            _explicitlyLocked = true;
            ClearKey();
            _manifest = null;
            State = VaultState.Locked;
        }
        finally
        {
            _gate.Release();
        }

        _options.Events.OnLocked?.Invoke();
        Locked?.Invoke(this, EventArgs.Empty);
    }

    public async Task<VaultFileInfo> WriteAsync(string path, Stream content, VaultWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var plaintext = await ReadPlaintextAsync(content, cancellationToken).ConfigureAwait(false);
        return await WriteCoreAsync(path, plaintext, options, cancellationToken).ConfigureAwait(false);
    }

    public Task<VaultFileInfo> WriteAsync(string path, byte[] content, VaultWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        return WriteCoreAsync(path, content, options, cancellationToken);
    }

    public Task<VaultFileInfo> WriteTextAsync(string path, string text, VaultWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        options ??= new VaultWriteOptions();
        options.ContentType ??= "text/plain; charset=utf-8";
        return WriteCoreAsync(path, Encoding.UTF8.GetBytes(text), options, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var bytes = await ReadAsync(path, cancellationToken).ConfigureAwait(false);
        return new MemoryStream(bytes, writable: false);
    }

    public async Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            var logical = VaultPath.Normalize(path);
            var entry = RequireEntry(logical);
            RemoveIfExpired(entry, delete: true);
            var payload = await File.ReadAllBytesAsync(PhysicalPath(entry.Id), cancellationToken).ConfigureAwait(false);
            var plaintext = VaultCrypto.Decrypt(payload, _masterKey!, System.Text.Encoding.UTF8.GetBytes(entry.Id));
            entry.AccessedAt = _clock.UtcNow;
            SaveManifest();
            return plaintext;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
    {
        var bytes = await ReadAsync(path, cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(bytes);
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return await GetInfoAsync(path, cancellationToken).ConfigureAwait(false) is not null;
    }

    public async Task<VaultFileInfo?> GetInfoAsync(string path, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            var logical = VaultPath.Normalize(path);
            var entry = FindEntry(logical);
            if (entry is null)
            {
                return null;
            }

            if (IsExpired(entry))
            {
                DeleteEntry(entry);
                SaveManifest();
                return null;
            }

            return entry.ToInfo();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<VaultFileInfo>> ListAsync(string? directory = null, bool recursive = true, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<VaultFileInfo> files;
        List<string> purged;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            purged = AutoPurgeIfDue(force: false);
            files = Manifest.Files
                .Where(e => !IsExpired(e) && VaultPath.IsUnderDirectory(e.Path, directory, recursive))
                .OrderBy(e => e.Path, StringComparer.Ordinal)
                .Select(e => e.ToInfo())
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }

        RaisePurged(purged);
        return files;
    }

    public async Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        string? deleted = null;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            var logical = VaultPath.Normalize(path);
            var entry = FindEntry(logical);
            if (entry is null)
            {
                return false;
            }

            DeleteEntry(entry);
            SaveManifest();
            deleted = logical;
        }
        finally
        {
            _gate.Release();
        }

        if (deleted is not null)
        {
            RaiseDeleted(deleted);
        }

        return deleted is not null;
    }

    public async Task<VaultFileInfo> SetExpirationAsync(string path, DateTimeOffset? expiresAt, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            var entry = RequireEntry(VaultPath.Normalize(path));
            RemoveIfExpired(entry, delete: true);
            entry.ExpiresAt = expiresAt;
            entry.AccessedAt = _clock.UtcNow;
            SaveManifest();
            return entry.ToInfo();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<VaultFileInfo> TouchAsync(string path, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            var entry = RequireEntry(VaultPath.Normalize(path));
            RemoveIfExpired(entry, delete: true);
            entry.AccessedAt = _clock.UtcNow;
            SaveManifest();
            return entry.ToInfo();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        List<string> removed;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            removed = PurgeExpiredCore();
            _lastAutoPurge = _clock.UtcNow;
        }
        finally
        {
            _gate.Release();
        }

        if (removed.Count > 0)
        {
            RaisePurged(removed);
        }

        return removed.Count;
    }

    public async Task ChangePassphraseAsync(string? currentPassphrase, string? newPassphrase, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            EnsureRoot();

            if (!IsUnlocked)
            {
                await UnlockUnlockedHeldAsync(currentPassphrase, cancellationToken).ConfigureAwait(false);
            }
            else if (File.Exists(WrapPath) && !string.IsNullOrWhiteSpace(currentPassphrase))
            {
                var probe = PassphraseWrap.Unwrap(await File.ReadAllBytesAsync(WrapPath, cancellationToken).ConfigureAwait(false), currentPassphrase);
                VaultCrypto.Zero(probe);
            }
            else if (File.Exists(WrapPath) && string.IsNullOrWhiteSpace(currentPassphrase))
            {
                throw new FileVaultException(FileVaultError.PassphraseRequired, "The current passphrase is required to change it.");
            }

            if (string.IsNullOrWhiteSpace(newPassphrase))
            {
                await _keys.SetAsync(_secureKeyName, Convert.ToBase64String(_masterKey!)).ConfigureAwait(false);
                if (File.Exists(WrapPath))
                {
                    SecureWipe.File(WrapPath, _options.SecureDelete);
                }
            }
            else
            {
                await File.WriteAllBytesAsync(WrapPath, PassphraseWrap.Wrap(_masterKey!, newPassphrase, _options.Pbkdf2Iterations), cancellationToken).ConfigureAwait(false);
                Protect(WrapPath);
                _keys.Remove(_secureKeyName);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DestroyAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            EnsureRoot();

            var filesDir = FilesDirectory;
            if (Directory.Exists(filesDir))
            {
                foreach (var file in Directory.GetFiles(filesDir))
                {
                    SecureWipe.File(file, _options.SecureDelete);
                }
            }

            SecureWipe.File(ManifestPath, _options.SecureDelete);
            SecureWipe.File(WrapPath, _options.SecureDelete);
            _keys.Remove(_secureKeyName);

            if (Directory.Exists(filesDir))
            {
                Directory.Delete(filesDir, recursive: true);
            }

            ClearKey();
            _manifest = null;
            State = VaultState.Destroyed;
        }
        catch (IOException ex)
        {
            throw new FileVaultException(FileVaultError.IoFailure, "The vault could not be destroyed.", ex);
        }
        finally
        {
            _gate.Release();
        }

        _options.Events.OnLocked?.Invoke();
        Locked?.Invoke(this, EventArgs.Empty);
    }

    public VaultStatistics GetStatistics()
    {
        _gate.Wait();
        try
        {
            if (!IsUnlocked || _manifest is null)
            {
                throw new FileVaultException(FileVaultError.Locked, "Unlock the vault before reading statistics.");
            }

            var files = _manifest.Files;
            return new VaultStatistics
            {
                FileCount = files.Count,
                PlaintextBytes = files.Sum(f => f.Size),
                CiphertextBytes = files.Sum(f => f.CipherSize),
                ExpiredCount = files.Count(IsExpired),
                OldestCreatedAt = files.Count == 0 ? null : files.Min(f => f.CreatedAt),
                NewestModifiedAt = files.Count == 0 ? null : files.Max(f => f.ModifiedAt)
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public void NotifyForeground()
    {
        if (!_options.AutoPurgeOnResume)
        {
            return;
        }

        _ = PurgeExpiredAsync();
    }

    public void NotifyBackground()
    {
        if (!_options.LockOnBackground)
        {
            return;
        }

        _explicitlyLocked = true;

        if (!_gate.Wait(TimeSpan.FromSeconds(2)))
        {
            return;
        }

        try
        {
            ThrowIfDisposed();
            _explicitlyLocked = true;
            ClearKey();
            _manifest = null;
            State = VaultState.Locked;
        }
        finally
        {
            _gate.Release();
        }

        _options.Events.OnLocked?.Invoke();
        Locked?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ClearKey();
        _manifest = null;
        _gate.Dispose();
    }

    async Task<VaultFileInfo> WriteCoreAsync(string path, byte[] plaintext, VaultWriteOptions? options, CancellationToken cancellationToken)
    {
        if (plaintext.LongLength > _options.MaxFileSizeBytes)
        {
            throw new FileVaultException(
                FileVaultError.FileTooLarge,
                $"The file is {plaintext.LongLength} bytes, which exceeds the {_options.MaxFileSizeBytes} byte limit.");
        }

        VaultFileInfo info;
        List<string> evicted = [];
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            evicted.AddRange(AutoPurgeIfDue(force: false));

            var logical = VaultPath.Normalize(path);
            var now = _clock.UtcNow;
            var existing = FindEntry(logical);
            var id = existing?.Id ?? Guid.NewGuid().ToString("N");
            var ciphertext = VaultCrypto.Encrypt(plaintext, _masterKey!, System.Text.Encoding.UTF8.GetBytes(id));

            evicted.AddRange(EnsureQuota(ciphertext.LongLength, existing, id));

            Directory.CreateDirectory(FilesDirectory);
            var physical = PhysicalPath(id);
            var temp = physical + ".tmp";
            await File.WriteAllBytesAsync(temp, ciphertext, cancellationToken).ConfigureAwait(false);
            Protect(temp);
            File.Move(temp, physical, overwrite: true);
            Protect(physical);

            var expiresAt = ResolveExpiration(options, now);
            var metadata = options?.Metadata is { } map
                ? new Dictionary<string, string>(map, StringComparer.Ordinal)
                : existing?.Metadata is { } previous
                    ? new Dictionary<string, string>(previous, StringComparer.Ordinal)
                    : new Dictionary<string, string>(StringComparer.Ordinal);

            if (existing is null)
            {
                existing = new ManifestEntry { Id = id, Path = logical, CreatedAt = now };
                Manifest.Files.Add(existing);
            }

            existing.Size = plaintext.LongLength;
            existing.CipherSize = ciphertext.LongLength;
            existing.ContentType = options?.ContentType ?? existing.ContentType;
            existing.ModifiedAt = now;
            existing.AccessedAt = now;
            existing.ExpiresAt = expiresAt;
            existing.IsPinned = options?.Pin ?? existing.IsPinned;
            existing.Metadata = metadata;

            SaveManifest();
            info = existing.ToInfo();
        }
        catch (IOException ex)
        {
            throw new FileVaultException(FileVaultError.IoFailure, "The vault file could not be written.", ex);
        }
        finally
        {
            _gate.Release();
        }

        if (evicted.Count > 0)
        {
            RaisePurged(evicted);
        }

        _options.Events.OnWritten?.Invoke(info);
        FileWritten?.Invoke(this, new VaultFileEventArgs(info.Path, info));
        return info;
    }

    async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (_explicitlyLocked)
        {
            throw new FileVaultException(FileVaultError.Locked, "Unlock the vault before using it.");
        }

        if (IsUnlocked)
        {
            return;
        }

        if (State == VaultState.Destroyed)
        {
            // A new unlock after destroy creates a fresh vault.
        }

        if (_options.RequirePassphrase || File.Exists(WrapPathSafe))
        {
            throw new FileVaultException(FileVaultError.Locked, "Unlock the vault with a passphrase before using it.");
        }

        await UnlockUnlockedHeldAsync(passphrase: null, cancellationToken).ConfigureAwait(false);
    }

    async Task UnlockUnlockedHeldAsync(string? passphrase, CancellationToken cancellationToken)
    {
        EnsureRoot();
        var wrapPath = WrapPath;
        var hasWrap = File.Exists(wrapPath);
        var stored = await _keys.GetAsync(_secureKeyName).ConfigureAwait(false);

        if (hasWrap)
        {
            if (string.IsNullOrWhiteSpace(passphrase))
            {
                throw new FileVaultException(FileVaultError.PassphraseRequired, "This vault is protected by a passphrase.");
            }

            _masterKey = PassphraseWrap.Unwrap(await File.ReadAllBytesAsync(wrapPath, cancellationToken).ConfigureAwait(false), passphrase);
        }
        else if (stored is not null)
        {
            _masterKey = DecodeKey(stored);
        }
        else if (!string.IsNullOrWhiteSpace(passphrase) || _options.RequirePassphrase)
        {
            if (string.IsNullOrWhiteSpace(passphrase))
            {
                throw new FileVaultException(FileVaultError.PassphraseRequired, "A passphrase is required to create this vault.");
            }

            _masterKey = VaultCrypto.GenerateKey();
            await File.WriteAllBytesAsync(wrapPath, PassphraseWrap.Wrap(_masterKey, passphrase, _options.Pbkdf2Iterations), cancellationToken).ConfigureAwait(false);
            Protect(wrapPath);
        }
        else
        {
            _masterKey = VaultCrypto.GenerateKey();
            await _keys.SetAsync(_secureKeyName, Convert.ToBase64String(_masterKey)).ConfigureAwait(false);
        }

        LoadManifest();
        State = VaultState.Unlocked;
    }

    void EnsureRoot()
    {
        var name = SanitizeVaultName(_options.VaultName);
        _root = _storage.ResolveRoot(name, _options.ExcludeFromBackup, _options.RootDirectory);
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(FilesDirectory);
        if (_options.ExcludeFromBackup)
        {
            _storage.ExcludeFromBackup(_root);
        }
    }

    void LoadManifest()
    {
        if (!File.Exists(ManifestPath))
        {
            _manifest = new ManifestDocument();
            return;
        }

        try
        {
            var payload = File.ReadAllBytes(ManifestPath);
            var json = VaultCrypto.Decrypt(payload, _masterKey!, "manifest"u8);
            _manifest = JsonSerializer.Deserialize(json, ManifestJsonContext.Default.ManifestDocument)
                ?? new ManifestDocument();
        }
        catch (FileVaultException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FileVaultException(FileVaultError.DecryptionFailed, "The vault manifest could not be read.", ex);
        }

        var known = Manifest.Files.Select(f => f.Id).ToHashSet(StringComparer.Ordinal);
        if (Directory.Exists(FilesDirectory))
        {
            foreach (var file in Directory.GetFiles(FilesDirectory, "*.fv"))
            {
                var id = Path.GetFileNameWithoutExtension(file);
                if (!known.Contains(id))
                {
                    SecureWipe.File(file, _options.SecureDelete);
                }
            }
        }
    }

    void SaveManifest()
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(Manifest, ManifestJsonContext.Default.ManifestDocument);
        var payload = VaultCrypto.Encrypt(json, _masterKey!, "manifest"u8);
        var temp = ManifestPath + ".tmp";
        File.WriteAllBytes(temp, payload);
        Protect(temp);
        File.Move(temp, ManifestPath, overwrite: true);
        Protect(ManifestPath);
    }

    ManifestDocument Manifest => _manifest ?? throw new FileVaultException(FileVaultError.Locked, "The vault is not unlocked.");

    ManifestEntry? FindEntry(string path) =>
        Manifest.Files.FirstOrDefault(f => f.Path.Equals(path, StringComparison.Ordinal));

    ManifestEntry RequireEntry(string path) =>
        FindEntry(path) ?? throw new FileVaultException(FileVaultError.NotFound, $"No vault file exists at '{path}'.");

    bool IsExpired(ManifestEntry entry)
    {
        var now = _clock.UtcNow;
        if (entry.ExpiresAt is { } expires && expires <= now)
        {
            return true;
        }

        return _options.MaxIdleTime is { } idle && now - entry.AccessedAt >= idle;
    }

    void RemoveIfExpired(ManifestEntry entry, bool delete)
    {
        if (!IsExpired(entry))
        {
            return;
        }

        if (delete)
        {
            var path = entry.Path;
            DeleteEntry(entry);
            SaveManifest();
            throw new FileVaultException(FileVaultError.Expired, $"The vault file '{path}' has expired.");
        }
    }

    void DeleteEntry(ManifestEntry entry)
    {
        SecureWipe.File(PhysicalPath(entry.Id), _options.SecureDelete);
        Manifest.Files.Remove(entry);
    }

    List<string> PurgeExpiredCore()
    {
        var removed = new List<string>();
        foreach (var entry in Manifest.Files.Where(IsExpired).ToArray())
        {
            removed.Add(entry.Path);
            DeleteEntry(entry);
        }

        if (removed.Count > 0)
        {
            SaveManifest();
        }

        return removed;
    }

    List<string> AutoPurgeIfDue(bool force)
    {
        if (!_options.AutoPurgeOnResume && !force)
        {
            return [];
        }

        if (!force && _clock.UtcNow - _lastAutoPurge < _options.AutoPurgeInterval)
        {
            return [];
        }

        var removed = PurgeExpiredCore();
        _lastAutoPurge = _clock.UtcNow;
        return removed;
    }

    List<string> EnsureQuota(long incomingCipherSize, ManifestEntry? _, string incomingId)
    {
        if (_options.MaxVaultSizeBytes is not { } max)
        {
            return [];
        }

        var current = Manifest.Files.Where(f => f.Id != incomingId).Sum(f => f.CipherSize);
        var projected = current + incomingCipherSize;
        if (projected <= max)
        {
            return [];
        }

        if (_options.EvictionPolicy == VaultEvictionPolicy.None)
        {
            throw new FileVaultException(
                FileVaultError.QuotaExceeded,
                $"The write needs {incomingCipherSize} bytes and would exceed the {max} byte vault quota.");
        }

        IEnumerable<ManifestEntry> candidates = Manifest.Files.Where(f => !f.IsPinned && f.Id != incomingId);
        candidates = _options.EvictionPolicy == VaultEvictionPolicy.Oldest
            ? candidates.OrderBy(f => f.CreatedAt)
            : candidates.OrderBy(f => f.AccessedAt);

        var evicted = new List<string>();
        foreach (var entry in candidates.ToArray())
        {
            if (projected <= max)
            {
                break;
            }

            projected -= entry.CipherSize;
            evicted.Add(entry.Path);
            DeleteEntry(entry);
        }

        if (projected > max)
        {
            throw new FileVaultException(
                FileVaultError.QuotaExceeded,
                $"The write needs {incomingCipherSize} bytes and would exceed the {max} byte vault quota.");
        }

        return evicted;
    }

    DateTimeOffset? ResolveExpiration(VaultWriteOptions? options, DateTimeOffset now)
    {
        if (options?.ExpiresAt is { } absolute)
        {
            return absolute;
        }

        var ttl = options?.TimeToLive ?? _options.DefaultTimeToLive;
        return ttl is { } lifetime ? now.Add(lifetime) : null;
    }

    async Task<byte[]> ReadPlaintextAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content.CanSeek && content.Length - content.Position > _options.MaxFileSizeBytes)
        {
            throw new FileVaultException(
                FileVaultError.FileTooLarge,
                $"The file is {content.Length - content.Position} bytes, which exceeds the {_options.MaxFileSizeBytes} byte limit.");
        }

        using var buffer = new MemoryStream();
        var rented = ArrayPool<byte>.Shared.Rent(81_920);
        try
        {
            int read;
            while ((read = await content.ReadAsync(rented.AsMemory(0, rented.Length), cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + read > _options.MaxFileSizeBytes)
                {
                    throw new FileVaultException(
                        FileVaultError.FileTooLarge,
                        $"The file exceeds the {_options.MaxFileSizeBytes} byte limit.");
                }

                buffer.Write(rented, 0, read);
            }

            return buffer.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
    }

    void RaiseDeleted(string path)
    {
        _options.Events.OnDeleted?.Invoke(path);
        FileDeleted?.Invoke(this, new VaultFileEventArgs(path, info: null));
    }

    void RaiseUnlocked()
    {
        _options.Events.OnUnlocked?.Invoke();
        Unlocked?.Invoke(this, EventArgs.Empty);
    }

    void RaisePurged(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            return;
        }

        _options.Events.OnPurged?.Invoke(paths.Count);
        ExpiredPurged?.Invoke(this, new VaultPurgedEventArgs(paths.Count, paths));
        foreach (var path in paths)
        {
            _options.Events.OnDeleted?.Invoke(path);
            FileDeleted?.Invoke(this, new VaultFileEventArgs(path, info: null));
        }
    }

    void Protect(string path)
    {
        try
        {
            _storage.ProtectFile(path);
            if (_options.ExcludeFromBackup)
            {
                _storage.ExcludeFromBackup(path);
            }
        }
        catch (Exception)
        {
            // Platform protection is best-effort on top of encryption.
        }
    }

    void ClearKey()
    {
        VaultCrypto.Zero(_masterKey);
        _masterKey = null;
    }

    void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (State == VaultState.Destroyed && !IsUnlockAfterDestroyAllowed())
        {
            // Destroyed is recoverable via UnlockAsync, which resets state.
        }
    }

    static bool IsUnlockAfterDestroyAllowed() => true;

    static byte[] DecodeKey(string stored)
    {
        try
        {
            var key = Convert.FromBase64String(stored);
            if (key.Length != VaultCrypto.KeySize)
            {
                throw new FileVaultException(FileVaultError.DecryptionFailed, "The stored master key is not 256 bits.");
            }

            return key;
        }
        catch (FormatException ex)
        {
            throw new FileVaultException(FileVaultError.DecryptionFailed, "The stored master key is not valid.", ex);
        }
    }

    static string SanitizeVaultName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return FileVaultDefaults.VaultName;
        }

        var cleaned = name.Trim();
        foreach (var ch in Path.GetInvalidFileNameChars())
        {
            cleaned = cleaned.Replace(ch, '_');
        }

        return cleaned;
    }

    string Root => _root ?? throw new FileVaultException(FileVaultError.IoFailure, "The vault root has not been resolved.");

    string ManifestPath => Path.Combine(Root, ManifestFileName);

    string WrapPath => Path.Combine(Root, WrapFileName);

    string WrapPathSafe
    {
        get
        {
            try
            {
                EnsureRoot();
                return WrapPath;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    string FilesDirectory => Path.Combine(Root, FilesFolderName);

    string PhysicalPath(string id) => Path.Combine(FilesDirectory, id + ".fv");
}
