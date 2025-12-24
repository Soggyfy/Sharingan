using Sharingan.Abstractions;
using Sharingan.Internal;
using Sharingan.Serialization;
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace Sharingan.Providers.Xml;

/// <summary>
/// An XML file-based settings provider using System.Xml.Linq.
/// Suitable for applications requiring XML format for compatibility or integration.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="XmlFileSettingsProvider"/> stores settings in XML format,
/// offering advantages for specific scenarios:
/// <list type="bullet">
/// <item><description>Schema validation support for enterprise environments</description></item>
/// <item><description>Integration with XML-based tooling and workflows</description></item>
/// <item><description>XPath querying capabilities for advanced scenarios</description></item>
/// <item><description>Familiar format for .NET Framework migration</description></item>
/// </list>
/// </para>
/// <para>
/// Hierarchical keys (e.g., "database.connection.host") are stored as nested XML elements.
/// Example structure:
/// <code>
/// &lt;settings&gt;
///   &lt;database&gt;
///     &lt;connection&gt;
///       &lt;host&gt;localhost&lt;/host&gt;
///     &lt;/connection&gt;
///   &lt;/database&gt;
/// &lt;/settings&gt;
/// </code>
/// </para>
/// </remarks>
/// <example>
/// Using the XML provider:
/// <code>
/// var provider = new XmlFileSettingsProvider(new XmlProviderOptions
/// {
///     FilePath = "settings.xml",
///     RootElementName = "configuration"
/// });
/// 
/// provider.Set("app.theme", "dark");
/// provider.Flush();
/// </code>
/// </example>
/// <seealso cref="XmlProviderOptions"/>
/// <seealso cref="SharinganBuilderExtensions.UseXmlFile"/>
public class XmlFileSettingsProvider : ISettingsProvider
{
    /// <summary>
    /// Thread-safe cache of settings.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Semaphore for exclusive file access.
    /// </summary>
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <summary>
    /// Configuration options for this provider.
    /// </summary>
    private readonly XmlProviderOptions _options;

    /// <summary>
    /// Serializer for complex type conversion.
    /// </summary>
    private readonly ISettingsSerializer _serializer;

    /// <summary>
    /// The resolved absolute file path.
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
    /// <value>Returns "XmlFile:{filename}" for identification.</value>
    public string Name => $"XmlFile:{Path.GetFileName(_filePath)}";

    /// <inheritdoc />
    /// <value>Always returns <c>false</c> since XML files are writable.</value>
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
    /// Initializes a new instance of the <see cref="XmlFileSettingsProvider"/> class.
    /// </summary>
    /// <param name="options">Configuration options. If null, default options are used.</param>
    /// <param name="serializer">Optional custom serializer. If null, uses the default JSON serializer.</param>
    public XmlFileSettingsProvider(XmlProviderOptions? options = null, ISettingsSerializer? serializer = null)
    {
        _options = options ?? new XmlProviderOptions();
        _serializer = serializer ?? _options.Serializer ?? JsonSettingsSerializer.Default;
        _filePath = PathResolver.GetFilePath(_options.FilePath, _options.Scope, _options.ApplicationName, _options.OrganizationName);

        LoadFromFile();
    }

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        if (_cache.TryGetValue(key, out string? value))
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
        if (_cache.TryGetValue(key, out string? stored))
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
        string stringValue = ConvertToString(value);
        _cache[key] = stringValue;
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
        try { SaveToFile(); }
        finally { _fileLock.Release(); }
    }
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try { LoadFromFileInternal(); }
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
                XDocument doc = new(new XElement(_options.RootElementName));
                doc.Save(_filePath);
            }
            return;
        }

        try
        {
            XDocument doc = XDocument.Load(_filePath);
            if (doc.Root is null)
            {
                return;
            }

            foreach (XElement element in doc.Root.Elements())
            {
                // Support nested elements with dot notation
                LoadElement(element, string.Empty);
            }
        }
        catch { /* Ignore parse errors */ }
        _isDirty = false;
    }

    private void LoadElement(XElement element, string prefix)
    {
        string key = string.IsNullOrEmpty(prefix) ? element.Name.LocalName : $"{prefix}.{element.Name.LocalName}";

        if (element.HasElements)
        {
            // Has child elements, recurse
            foreach (XElement child in element.Elements())
            {
                LoadElement(child, key);
            }
        }
        else
        {
            // Leaf node, store value
            _cache[key] = element.Value;
        }
    }

    private void SaveToFile()
    {
        PathResolver.EnsureDirectoryExists(_filePath);

        XElement root = new(_options.RootElementName);

        foreach (KeyValuePair<string, string> kvp in _cache.OrderBy(k => k.Key))
        {
            AddToXml(root, kvp.Key.Split('.'), kvp.Value);
        }

        XDocument doc = new(new XDeclaration("1.0", "utf-8", "yes"), root);

        if (_options.UseAtomicWrites)
        {
            string temp = _filePath + ".tmp";
            doc.Save(temp);
            FileHelper.MoveWithOverwrite(temp, _filePath);
        }
        else
        {
            doc.Save(_filePath);
        }
        _isDirty = false;
    }

    private static void AddToXml(XElement parent, string[] parts, string value)
    {
        if (parts.Length == 0)
        {
            return;
        }

        XElement current = parent;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            XElement? existing = current.Element(parts[i]);
            if (existing is null)
            {
                existing = new XElement(parts[i]);
                current.Add(existing);
            }
            current = existing;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        XElement leaf = new(parts[^1], value);
#else
        XElement leaf = new(parts[parts.Length - 1], value);
#endif
        current.Add(leaf);
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
