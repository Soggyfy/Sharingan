using Sharingan.Abstractions;
using Sharingan.Internal;
using Sharingan.Serialization;
using System.Collections.Concurrent;
using System.Text;

namespace Sharingan.Providers.Ini;

/// <summary>
/// An INI file-based settings provider supporting section-based organization.
/// Keys can be in format "Section.Key" or just "Key" (uses default section).
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IniFileSettingsProvider"/> stores settings in the classic INI file format
/// with [Section] headers and key=value pairs. This format is human-readable and widely
/// supported by configuration management tools.
/// </para>
/// <para>
/// Key format conventions:
/// <list type="bullet">
/// <item><description>"Section.Key" - Uses the specified section (e.g., "Database.Host")</description></item>
/// <item><description>"Key" - Uses the default section configured in options</description></item>
/// </list>
/// </para>
/// <para>
/// Example INI file:
/// <code>
/// [Settings]
/// theme=dark
/// volume=75
/// 
/// [Database]
/// host=localhost
/// port=5432
/// </code>
/// </para>
/// </remarks>
/// <example>
/// Using the INI provider:
/// <code>
/// var provider = new IniFileSettingsProvider(new IniProviderOptions
/// {
///     FilePath = "config.ini",
///     DefaultSection = "Application"
/// });
/// 
/// // Stored in [Application] section
/// provider.Set("theme", "dark");
/// 
/// // Stored in [Database] section
/// provider.Set("Database.Host", "localhost");
/// 
/// provider.Flush();
/// </code>
/// </example>
/// <seealso cref="IniProviderOptions"/>
/// <seealso cref="SharinganBuilderExtensions.UseIniFile"/>
public partial class IniFileSettingsProvider : ISettingsProvider
{
    /// <summary>
    /// Thread-safe dictionary of sections, each containing key-value pairs.
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Semaphore for exclusive file access during read/write operations.
    /// </summary>
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <summary>
    /// Configuration options for this provider.
    /// </summary>
    private readonly IniProviderOptions _options;

    /// <summary>
    /// Serializer for converting complex types to string values.
    /// </summary>
    private readonly ISettingsSerializer _serializer;

    /// <summary>
    /// The resolved absolute file path for the INI settings file.
    /// </summary>
    private readonly string _filePath;

    /// <summary>
    /// Tracks whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Tracks whether there are unsaved changes.
    /// </summary>
    private bool _isDirty;

    /// <inheritdoc />
    /// <value>Returns "IniFile:{filename}" for identification.</value>
    public string Name => $"IniFile:{Path.GetFileName(_filePath)}";

    /// <inheritdoc />
    /// <value>Always returns <c>false</c> since INI files are writable.</value>
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public int Priority => _options.Priority;

    /// <inheritdoc />
    public SettingsScope Scope => _options.Scope;

    /// <inheritdoc />
    /// <value>Returns the total count of settings across all sections.</value>
    public int Count => _sections.Values.Sum(s => s.Count);

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniFileSettingsProvider"/> class.
    /// </summary>
    /// <param name="options">Configuration options for file path, scope, and behavior. If null, default options are used.</param>
    /// <param name="serializer">Optional custom serializer for complex type conversion. If null, uses the default JSON serializer.</param>
    public IniFileSettingsProvider(IniProviderOptions? options = null, ISettingsSerializer? serializer = null)
    {
        _options = options ?? new IniProviderOptions();
        _serializer = serializer ?? _options.Serializer ?? JsonSettingsSerializer.Default;
        _filePath = PathResolver.GetFilePath(_options.FilePath, _options.Scope, _options.ApplicationName, _options.OrganizationName);

        LoadFromFile();
    }

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        (string? section, string? name) = ParseKey(key);
        if (_sections.TryGetValue(section, out ConcurrentDictionary<string, string>? sectionData) && sectionData.TryGetValue(name, out string? value))
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
        (string? section, string? name) = ParseKey(key);
        if (_sections.TryGetValue(section, out ConcurrentDictionary<string, string>? sectionData) && sectionData.TryGetValue(name, out string? strValue))
        {
            value = ConvertValue<T>(strValue, default);
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
        (string? section, string? name) = ParseKey(key);
        ConcurrentDictionary<string, string> sectionData = _sections.GetOrAdd(section, _ => new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        string stringValue = ConvertToString(value);
        sectionData[name] = stringValue;
        _isDirty = true;
        OnSettingsChanged(new SettingsChangedEventArgs(key, SettingsChangeType.Modified, null, value, Name));
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        (string? section, string? name) = ParseKey(key);
        if (_sections.TryGetValue(section, out ConcurrentDictionary<string, string>? sectionData) && sectionData.TryRemove(name, out _))
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
        _sections.Clear();
        _isDirty = true;
        OnSettingsChanged(SettingsChangedEventArgs.Cleared(Name));
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        (string? section, string? name) = ParseKey(key);
        return _sections.TryGetValue(section, out ConcurrentDictionary<string, string>? sectionData) && sectionData.ContainsKey(name);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllKeys()
    {
        foreach (KeyValuePair<string, ConcurrentDictionary<string, string>> section in _sections)
        {
            foreach (string key in section.Value.Keys)
            {
                yield return section.Key == _options.DefaultSection ? key : $"{section.Key}.{key}";
            }
        }
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

    // Async operations
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

    private (string Section, string Key) ParseKey(string key)
    {
        int dotIndex = key.IndexOf('.');
        if (dotIndex > 0)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            return (key[..dotIndex], key[(dotIndex + 1)..]);
#else
            return (key.Substring(0, dotIndex), key.Substring(dotIndex + 1));
#endif
        }
        return (_options.DefaultSection, key);
    }

    private void LoadFromFile()
    {
        _fileLock.Wait();
        try { LoadFromFileInternal(); }
        finally { _fileLock.Release(); }
    }

    private void LoadFromFileInternal()
    {
        _sections.Clear();
        if (!File.Exists(_filePath))
        {
            if (_options.CreateIfNotExists)
            {
                PathResolver.EnsureDirectoryExists(_filePath);
                File.WriteAllText(_filePath, $"[{_options.DefaultSection}]\n");
            }
            return;
        }

        string currentSection = _options.DefaultSection;
        foreach (string line in File.ReadAllLines(_filePath))
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
            {
                continue;
            }

            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                currentSection = trimmed[1..^1].Trim();
#else
                currentSection = trimmed.Substring(1, trimmed.Length - 2).Trim();
#endif
                continue;
            }

            int eqIndex = trimmed.IndexOf('=');
            if (eqIndex <= 0)
            {
                continue;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string key = trimmed[..eqIndex].Trim();
            string value = trimmed[(eqIndex + 1)..].Trim();
#else
            string key = trimmed.Substring(0, eqIndex).Trim();
            string value = trimmed.Substring(eqIndex + 1).Trim();
#endif

            ConcurrentDictionary<string, string> sectionData = _sections.GetOrAdd(currentSection, _ => new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            sectionData[key] = value;
        }
        _isDirty = false;
    }

    private async Task LoadFromFileInternalAsync(CancellationToken ct)
    {
        _sections.Clear();
        if (!File.Exists(_filePath))
        {
            return;
        }

        string[] lines = await FileHelper.ReadAllLinesAsync(_filePath, ct).ConfigureAwait(false);
        string currentSection = _options.DefaultSection;
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
            {
                continue;
            }

            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                currentSection = trimmed[1..^1].Trim();
#else
                currentSection = trimmed.Substring(1, trimmed.Length - 2).Trim();
#endif
                continue;
            }

            int eqIndex = trimmed.IndexOf('=');
            if (eqIndex <= 0)
            {
                continue;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string key = trimmed[..eqIndex].Trim();
            string value = trimmed[(eqIndex + 1)..].Trim();
#else
            string key = trimmed.Substring(0, eqIndex).Trim();
            string value = trimmed.Substring(eqIndex + 1).Trim();
#endif

            ConcurrentDictionary<string, string> sectionData = _sections.GetOrAdd(currentSection, _ => new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            sectionData[key] = value;
        }
        _isDirty = false;
    }

    private void SaveToFile()
    {
        PathResolver.EnsureDirectoryExists(_filePath);
        StringBuilder sb = new();
        foreach (KeyValuePair<string, ConcurrentDictionary<string, string>> section in _sections.OrderBy(s => s.Key))
        {
            sb.AppendLine($"[{section.Key}]");
            foreach (KeyValuePair<string, string> kvp in section.Value.OrderBy(k => k.Key))
            {
                sb.AppendLine($"{kvp.Key}={kvp.Value}");
            }
            sb.AppendLine();
        }
        File.WriteAllText(_filePath, sb.ToString());
        _isDirty = false;
    }

    private async Task SaveToFileAsync(CancellationToken ct)
    {
        PathResolver.EnsureDirectoryExists(_filePath);
        StringBuilder sb = new();
        foreach (KeyValuePair<string, ConcurrentDictionary<string, string>> section in _sections.OrderBy(s => s.Key))
        {
            sb.AppendLine($"[{section.Key}]");
            foreach (KeyValuePair<string, string> kvp in section.Value.OrderBy(k => k.Key))
            {
                sb.AppendLine($"{kvp.Key}={kvp.Value}");
            }
            sb.AppendLine();
        }
        await FileHelper.WriteAllTextAsync(_filePath, sb.ToString(), ct).ConfigureAwait(false);
        _isDirty = false;
    }

    private T? ConvertValue<T>(string value, T? defaultValue)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }

        if (typeof(T) == typeof(int) && int.TryParse(value, out int i))
        {
            return (T)(object)i;
        }

        if (typeof(T) == typeof(long) && long.TryParse(value, out long l))
        {
            return (T)(object)l;
        }

        if (typeof(T) == typeof(bool) && bool.TryParse(value, out bool b))
        {
            return (T)(object)b;
        }

        if (typeof(T) == typeof(double) && double.TryParse(value, out double d))
        {
            return (T)(object)d;
        }

        try { return _serializer.Deserialize<T>(value) ?? defaultValue; } catch { return defaultValue; }
    }

    private string ConvertToString<T>(T value)
    {
        if (value is string s)
        {
            return s;
        }

        if (value is int or long or bool or double or float or decimal)
        {
            return value.ToString() ?? "";
        }

        return _serializer.Serialize(value);
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
