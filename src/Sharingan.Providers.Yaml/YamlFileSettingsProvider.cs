using Sharingan.Abstractions;
using Sharingan.Internal;
using Sharingan.Serialization;
using System.Collections.Concurrent;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Sharingan.Providers.Yaml;

/// <summary>
/// A YAML file-based settings provider using the YamlDotNet library.
/// YAML provides excellent readability and is popular in cloud-native environments.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="YamlFileSettingsProvider"/> stores settings in YAML format,
/// which is widely used in modern DevOps and configuration management:
/// <list type="bullet">
/// <item><description>Human-readable with clean, indentation-based syntax</description></item>
/// <item><description>Native support for comments for documentation</description></item>
/// <item><description>Popular format for Kubernetes, Docker Compose, and CI/CD pipelines</description></item>
/// <item><description>Excellent for complex nested configuration</description></item>
/// </list>
/// </para>
/// <para>
/// Hierarchical keys (e.g., "database.connection.host") are stored as nested YAML mappings.
/// Example:
/// <code>
/// database:
///   connection:
///     host: localhost
///     port: 5432
/// </code>
/// </para>
/// </remarks>
/// <example>
/// Using the YAML provider:
/// <code>
/// var provider = new YamlFileSettingsProvider(new YamlProviderOptions
/// {
///     FilePath = "config.yaml"
/// });
/// 
/// provider.Set("database.host", "localhost");
/// provider.Set("database.port", 5432);
/// provider.Flush();
/// </code>
/// </example>
/// <seealso cref="YamlProviderOptions"/>
/// <seealso cref="SharinganBuilderExtensions.UseYamlFile"/>
public class YamlFileSettingsProvider : ISettingsProvider
{
    /// <summary>
    /// Thread-safe cache of settings.
    /// </summary>
    private readonly ConcurrentDictionary<string, object?> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Semaphore for exclusive file access.
    /// </summary>
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <summary>
    /// Configuration options for this provider.
    /// </summary>
    private readonly YamlProviderOptions _options;

    /// <summary>
    /// Serializer for complex type conversion.
    /// </summary>
    private readonly ISettingsSerializer _serializer;

    /// <summary>
    /// The resolved absolute file path.
    /// </summary>
    private readonly string _filePath;

    /// <summary>
    /// YamlDotNet serializer for writing YAML content.
    /// </summary>
    private readonly ISerializer _yamlSerializer;

    /// <summary>
    /// YamlDotNet deserializer for reading YAML content.
    /// </summary>
    private readonly IDeserializer _yamlDeserializer;

    /// <summary>
    /// Tracks whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Tracks whether there are unsaved changes.
    /// </summary>
    private bool _isDirty;

    /// <inheritdoc />
    /// <value>Returns "YamlFile:{filename}" for identification.</value>
    public string Name => $"YamlFile:{Path.GetFileName(_filePath)}";

    /// <inheritdoc />
    /// <value>Always returns <c>false</c> since YAML files are writable.</value>
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public int Priority => _options.Priority;

    /// <inheritdoc />
    public SettingsScope Scope => _options.Scope;

    /// <inheritdoc />
    public int Count => _cache.Count;

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlFileSettingsProvider"/> class.
    /// </summary>
    /// <param name="options">Configuration options. If null, default options are used.</param>
    /// <param name="serializer">Optional custom serializer. If null, uses the default JSON serializer.</param>
    public YamlFileSettingsProvider(YamlProviderOptions? options = null, ISettingsSerializer? serializer = null)
    {
        _options = options ?? new YamlProviderOptions();
        _serializer = serializer ?? _options.Serializer ?? JsonSettingsSerializer.Default;
        _filePath = PathResolver.GetFilePath(_options.FilePath, _options.Scope, _options.ApplicationName, _options.OrganizationName);

        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        LoadFromFile();
    }

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        if (_cache.TryGetValue(key, out object? value) && value is not null)
        {
            return ConvertValue<T>(value, defaultValue);
        }
        return defaultValue;
    }

    /// <inheritdoc />
    public T? GetOrDefault<T>(string key)
    {
        return Get<T>(key, default!);
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T? value)
    {
        if (_cache.TryGetValue(key, out object? stored) && stored is not null)
        {
            value = ConvertValue<T>(stored, default);
            return true;
        }
        value = default;
        return false;
    }

    /// <inheritdoc />
    public T GetOrCreate<T>(string key, Func<T> factory)
    {
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
        _cache[key] = value;
        _isDirty = true;
        OnSettingsChanged(new SettingsChangedEventArgs(key, SettingsChangeType.Modified, null, value, Name));
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        if (_cache.TryRemove(key, out _))
        {
            _isDirty = true;
            OnSettingsChanged(new SettingsChangedEventArgs(key, SettingsChangeType.Removed, null, null, Name));
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
        try { SaveToFile(); }
        finally { _fileLock.Release(); }
    }

    /// <inheritdoc />
    public void Reload()
    {
        _fileLock.Wait();
        try { LoadFromFileInternal(); }
        finally { _fileLock.Release(); }
    }

    public Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken ct = default)
    {
        return Task.FromResult(Get(key, defaultValue));
    }

    public Task<T?> GetOrDefaultAsync<T>(string key, CancellationToken ct = default)
    {
        return Task.FromResult(GetOrDefault<T>(key));
    }

    public Task<T> GetOrCreateAsync<T>(string key, Func<T> factory, CancellationToken ct = default)
    {
        return Task.FromResult(GetOrCreate(key, factory));
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
    {
        if (TryGet<T>(key, out T? existing) && existing is not null)
        {
            return existing;
        }

        T? value = await factory(ct).ConfigureAwait(false);
        Set(key, value);
        return value;
    }
    public Task SetAsync<T>(string key, T value, CancellationToken ct = default) { Set(key, value); return Task.CompletedTask; }
    public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(Remove(key));
    }

    public Task ClearAsync(CancellationToken ct = default) { Clear(); return Task.CompletedTask; }
    public async Task FlushAsync(CancellationToken ct = default)
    {
        if (!_isDirty)
        {
            return;
        }

        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try { await SaveToFileAsync(ct).ConfigureAwait(false); }
        finally { _fileLock.Release(); }
    }
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try { await LoadFromFileInternalAsync(ct).ConfigureAwait(false); }
        finally { _fileLock.Release(); }
    }

    private void LoadFromFile()
    {
        _fileLock.Wait();
        try { LoadFromFileInternal(); }
        finally { _fileLock.Release(); }
    }

    private void LoadFromFileInternal()
    {
        _cache.Clear();
        if (!File.Exists(_filePath))
        {
            if (_options.CreateIfNotExists)
            {
                PathResolver.EnsureDirectoryExists(_filePath);
                File.WriteAllText(_filePath, "# Settings\n");
            }
            return;
        }

        string yaml = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return;
        }

        try
        {
            Dictionary<string, object?>? data = _yamlDeserializer.Deserialize<Dictionary<string, object?>>(yaml);
            if (data is not null)
            {
                foreach (KeyValuePair<string, object?> kvp in data)
                {
                    _cache[kvp.Key] = kvp.Value;
                }
            }
        }
        catch { /* Ignore parse errors */ }
        _isDirty = false;
    }

    private async Task LoadFromFileInternalAsync(CancellationToken ct)
    {
        _cache.Clear();
        if (!File.Exists(_filePath))
        {
            return;
        }

        string yaml = await FileHelper.ReadAllTextAsync(_filePath, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return;
        }

        try
        {
            Dictionary<string, object?>? data = _yamlDeserializer.Deserialize<Dictionary<string, object?>>(yaml);
            if (data is not null)
            {
                foreach (KeyValuePair<string, object?> kvp in data)
                {
                    _cache[kvp.Key] = kvp.Value;
                }
            }
        }
        catch { }
        _isDirty = false;
    }

    private void SaveToFile()
    {
        PathResolver.EnsureDirectoryExists(_filePath);
        Dictionary<string, object?> data = [];
        foreach (KeyValuePair<string, object?> kvp in _cache)
        {
            data[kvp.Key] = kvp.Value;
        }

        string yaml = _yamlSerializer.Serialize(data);

        if (_options.UseAtomicWrites)
        {
            string temp = _filePath + ".tmp";
            File.WriteAllText(temp, yaml);
            FileHelper.MoveWithOverwrite(temp, _filePath);
        }
        else
        {
            File.WriteAllText(_filePath, yaml);
        }
        _isDirty = false;
    }

    private async Task SaveToFileAsync(CancellationToken ct)
    {
        PathResolver.EnsureDirectoryExists(_filePath);
        Dictionary<string, object?> data = [];
        foreach (KeyValuePair<string, object?> kvp in _cache)
        {
            data[kvp.Key] = kvp.Value;
        }

        string yaml = _yamlSerializer.Serialize(data);

        if (_options.UseAtomicWrites)
        {
            string temp = _filePath + ".tmp";
            await FileHelper.WriteAllTextAsync(temp, yaml, ct).ConfigureAwait(false);
            FileHelper.MoveWithOverwrite(temp, _filePath);
        }
        else
        {
            await FileHelper.WriteAllTextAsync(_filePath, yaml, ct).ConfigureAwait(false);
        }
        _isDirty = false;
    }

    private T? ConvertValue<T>(object value, T? defaultValue)
    {
        if (value is T typed)
        {
            return typed;
        }

        if (value is string str)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)str;
            }

            try { return _serializer.Deserialize<T>(str) ?? defaultValue; } catch { return defaultValue; }
        }
        // For complex types from YAML, serialize then deserialize
        try
        {
            string json = _serializer.Serialize(value);
            return _serializer.Deserialize<T>(json) ?? defaultValue;
        }
        catch { return defaultValue; }
    }

    protected virtual void OnSettingsChanged(SettingsChangedEventArgs e)
    {
        SettingsChanged?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (!_disposed) { Flush(); _fileLock.Dispose(); _disposed = true; }
        GC.SuppressFinalize(this);
    }
}
