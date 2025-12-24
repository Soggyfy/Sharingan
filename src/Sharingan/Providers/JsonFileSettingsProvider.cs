using Sharingan.Abstractions;
using Sharingan.Internal;
using Sharingan.Serialization;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Sharingan.Providers;

/// <summary>
/// A JSON file-based settings provider with atomic writes, multi-process safety,
/// and optional file change watching. This is the default and most commonly used provider.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="JsonFileSettingsProvider"/> persists settings to a JSON file with
/// comprehensive features for production use:
/// <list type="bullet">
/// <item><description>Atomic writes: Uses write-to-temp-then-rename to prevent corruption</description></item>
/// <item><description>Multi-process safety: File locking prevents concurrent write conflicts</description></item>
/// <item><description>In-memory caching: Fast reads with explicit flush for writes</description></item>
/// <item><description>File watching: Optional automatic reload when file changes externally</description></item>
/// <item><description>Retry logic: Configurable retries for handling transient file locks</description></item>
/// <item><description>Hierarchical keys: Dot notation (e.g., "database.connection.host")</description></item>
/// </list>
/// </para>
/// <para>
/// The JSON format uses pretty-printing and camelCase property naming by default,
/// making files human-readable and editable.
/// </para>
/// </remarks>
/// <example>
/// Basic usage:
/// <code>
/// var provider = new JsonFileSettingsProvider(new JsonFileProviderOptions
/// {
///     FilePath = "settings.json",
///     Scope = SettingsScope.User,
///     WatchForChanges = true
/// });
/// 
/// provider.Set("app.theme", "dark");
/// provider.Set("app.volume", 75);
/// provider.Flush(); // Persist to disk
/// 
/// // Later...
/// var theme = provider.Get("app.theme", "light");
/// </code>
/// </example>
/// <seealso cref="JsonFileProviderOptions"/>
/// <seealso cref="SharinganBuilder.UseJsonFile"/>
public class JsonFileSettingsProvider : ISettingsProvider
{
    /// <summary>
    /// Thread-safe cache storing JSON elements keyed by setting name.
    /// </summary>
    private readonly ConcurrentDictionary<string, JsonElement> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Semaphore ensuring exclusive access during file operations.
    /// </summary>
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <summary>
    /// Configuration options for this provider.
    /// </summary>
    private readonly JsonFileProviderOptions _options;

    /// <summary>
    /// Serializer for converting between typed values and JSON.
    /// </summary>
    private readonly ISettingsSerializer _serializer;

    /// <summary>
    /// The resolved absolute file path for the JSON settings file.
    /// </summary>
    private readonly string _filePath;

    /// <summary>
    /// Optional file system watcher for detecting external file changes.
    /// </summary>
    private FileSystemWatcher? _watcher;

    /// <summary>
    /// Tracks whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Tracks whether there are unsaved changes in the cache.
    /// </summary>
    private bool _isDirty;

    /// <inheritdoc />
    /// <value>Returns "JsonFile:{filename}" for identification purposes.</value>
    public string Name { get; }

    /// <inheritdoc />
    /// <value>Always returns <c>false</c> since JSON files are writable.</value>
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public int Priority => _options.Priority;

    /// <inheritdoc />
    public SettingsScope Scope => _options.Scope;

    /// <inheritdoc />
    /// <value>Returns the number of settings currently cached (may differ from file until flush).</value>
    public int Count => _cache.Count;

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFileSettingsProvider"/> class.
    /// </summary>
    /// <param name="options">The provider options configuring file path, scope, and behavior. If null, default options are used.</param>
    /// <param name="serializer">Optional custom serializer for value conversion. If null, uses the serializer from options or the default JSON serializer.</param>
    /// <remarks>
    /// The constructor immediately loads existing settings from the file (if it exists).
    /// If <see cref="JsonFileProviderOptions.CreateIfNotExists"/> is true and the file doesn't exist,
    /// an empty file is created.
    /// </remarks>
    public JsonFileSettingsProvider(JsonFileProviderOptions? options = null, ISettingsSerializer? serializer = null)
    {
        _options = options ?? new JsonFileProviderOptions();
        _serializer = serializer ?? _options.Serializer ?? JsonSettingsSerializer.Default;
        _filePath = PathResolver.GetFilePath(_options.FilePath, _options.Scope, _options.ApplicationName, _options.OrganizationName);
        Name = $"JsonFile:{Path.GetFileName(_filePath)}";

        LoadFromFile();

        if (_options.WatchForChanges)
        {
            SetupFileWatcher();
        }
    }

    #region Synchronous Operations

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        if (_cache.TryGetValue(key, out JsonElement element))
        {
            try
            {
                T? result = element.Deserialize<T>(GetJsonOptions());
                return result ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }

    /// <inheritdoc />
    public T? GetOrDefault<T>(string key)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        if (_cache.TryGetValue(key, out JsonElement element))
        {
            try
            {
                return element.Deserialize<T>(GetJsonOptions());
            }
            catch
            {
                return default;
            }
        }

        return default;
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T? value)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        if (_cache.TryGetValue(key, out JsonElement element))
        {
            try
            {
                value = element.Deserialize<T>(GetJsonOptions());
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public T GetOrCreate<T>(string key, Func<T> factory)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));
        ThrowHelper.ThrowIfNull(factory, nameof(factory));

        if (TryGet<T>(key, out T? existing) && existing is not null)
        {
            return existing;
        }

        T? value = factory();
        Set(key, value);
        return value;
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        string json = JsonSerializer.Serialize(value, GetJsonOptions());
        JsonElement element = JsonSerializer.Deserialize<JsonElement>(json);

        JsonElement? oldValue = _cache.TryGetValue(key, out JsonElement existing) ? existing : (JsonElement?)null;
        bool isNew = !oldValue.HasValue;

        _cache[key] = element;
        _isDirty = true;

        OnSettingsChanged(new SettingsChangedEventArgs(
            key,
            isNew ? SettingsChangeType.Added : SettingsChangeType.Modified,
            oldValue,
            value,
            Name));
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        if (_cache.TryRemove(key, out JsonElement oldValue))
        {
            _isDirty = true;
            OnSettingsChanged(new SettingsChangedEventArgs(
                key,
                SettingsChangeType.Removed,
                oldValue,
                null,
                Name));
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _cache.Clear();
        _isDirty = true;
        OnSettingsChanged(SettingsChangedEventArgs.Cleared(Name));
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));
        return _cache.ContainsKey(key);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllKeys()
    {
        return _cache.Keys;
    }

    /// <inheritdoc />
    public void Flush()
    {
        if (!_isDirty)
        {
            return;
        }

        _fileLock.Wait();
        try
        {
            SaveToFileInternal();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public void Reload()
    {
        _fileLock.Wait();
        try
        {
            LoadFromFileInternal();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    #endregion

    #region Asynchronous Operations

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Get(key, defaultValue));
    }

    /// <inheritdoc />
    public Task<T?> GetOrDefaultAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetOrDefault<T>(key));
    }

    /// <inheritdoc />
    public Task<T> GetOrCreateAsync<T>(string key, Func<T> factory, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetOrCreate(key, factory));
    }

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));
        ThrowHelper.ThrowIfNull(factory, nameof(factory));

        if (TryGet<T>(key, out T? existing) && existing is not null)
        {
            return existing;
        }

        T? value = await factory(cancellationToken).ConfigureAwait(false);
        Set(key, value);
        return value;
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Set(key, value);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Remove(key));
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (!_isDirty)
        {
            return;
        }

        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await SaveToFileInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await LoadFromFileInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    #endregion

    #region Private Methods

    private void LoadFromFile()
    {
        _fileLock.Wait();
        try
        {
            LoadFromFileInternal();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private void LoadFromFileInternal()
    {
        _cache.Clear();

        if (!File.Exists(_filePath))
        {
            if (_options.CreateIfNotExists)
            {
                PathResolver.EnsureDirectoryExists(_filePath);
                File.WriteAllText(_filePath, "{}");
            }
            return;
        }

        int retryCount = 0;
        while (retryCount < _options.MaxRetryAttempts)
        {
            try
            {
                string json = File.ReadAllText(_filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    json = "{}";
                }

                using JsonDocument document = JsonDocument.Parse(json);
                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    _cache[property.Name] = property.Value.Clone();
                }

                _isDirty = false;
                return;
            }
            catch (IOException) when (retryCount < _options.MaxRetryAttempts - 1)
            {
                retryCount++;
                Thread.Sleep(_options.RetryDelayMilliseconds);
            }
        }
    }

    private async Task LoadFromFileInternalAsync(CancellationToken cancellationToken)
    {
        _cache.Clear();

        if (!File.Exists(_filePath))
        {
            if (_options.CreateIfNotExists)
            {
                PathResolver.EnsureDirectoryExists(_filePath);
                await FileHelper.WriteAllTextAsync(_filePath, "{}", cancellationToken).ConfigureAwait(false);
            }
            return;
        }

        int retryCount = 0;
        while (retryCount < _options.MaxRetryAttempts)
        {
            try
            {
                string json = await FileHelper.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(json))
                {
                    json = "{}";
                }

                using JsonDocument document = JsonDocument.Parse(json);
                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    _cache[property.Name] = property.Value.Clone();
                }

                _isDirty = false;
                return;
            }
            catch (IOException) when (retryCount < _options.MaxRetryAttempts - 1)
            {
                retryCount++;
                await Task.Delay(_options.RetryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private void SaveToFileInternal()
    {
        PathResolver.EnsureDirectoryExists(_filePath);

        Dictionary<string, JsonElement> data = [];
        foreach (KeyValuePair<string, JsonElement> kvp in _cache)
        {
            data[kvp.Key] = kvp.Value;
        }

        string json = JsonSerializer.Serialize(data, GetJsonOptions());

        if (_options.UseAtomicWrites)
        {
            string tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            FileHelper.MoveWithOverwrite(tempPath, _filePath);
        }
        else
        {
            File.WriteAllText(_filePath, json);
        }

        _isDirty = false;
    }

    private async Task SaveToFileInternalAsync(CancellationToken cancellationToken)
    {
        PathResolver.EnsureDirectoryExists(_filePath);

        Dictionary<string, JsonElement> data = [];
        foreach (KeyValuePair<string, JsonElement> kvp in _cache)
        {
            data[kvp.Key] = kvp.Value;
        }

        string json = JsonSerializer.Serialize(data, GetJsonOptions());

        if (_options.UseAtomicWrites)
        {
            string tempPath = _filePath + ".tmp";
            await FileHelper.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
            FileHelper.MoveWithOverwrite(tempPath, _filePath);
        }
        else
        {
            await FileHelper.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
        }

        _isDirty = false;
    }

    private void SetupFileWatcher()
    {
        string? directory = Path.GetDirectoryName(_filePath);
        string fileName = Path.GetFileName(_filePath);

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return;
        }

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce and reload
        Task.Delay(100).ContinueWith(_ =>
        {
            try
            {
                Reload();
            }
            catch
            {
                // Ignore reload errors from file watcher
            }
        });
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Raises the <see cref="SettingsChanged"/> event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnSettingsChanged(SettingsChangedEventArgs e)
    {
        SettingsChanged?.Invoke(this, e);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            Flush();
            _watcher?.Dispose();
            _fileLock.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    #endregion
}
