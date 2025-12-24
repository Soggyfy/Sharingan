using Sharingan.Abstractions;
using Sharingan.Internal;
using System.Collections;
using System.Collections.Concurrent;

namespace Sharingan.Providers;

/// <summary>
/// A read-only settings provider that reads values from environment variables.
/// Ideal for deployment-time configuration, Docker containers, CI/CD pipelines,
/// and twelve-factor application patterns.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EnvironmentSettingsProvider"/> provides read-only access to system
/// environment variables as settings. It is typically used with the highest priority
/// in composite configurations to allow environment variables to override file-based settings.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item><description>Optional prefix filtering (e.g., "MYAPP_" to include only app-specific variables)</description></item>
/// <item><description>Automatic key name conversion: underscores become dots (DATABASE_HOST → database.host)</description></item>
/// <item><description>Case-insensitive key matching</description></item>
/// <item><description>Values cached at construction time (call Reload to refresh)</description></item>
/// <item><description>Read-only: write operations throw <see cref="NotSupportedException"/></description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Using environment variables with prefix:
/// <code>
/// // Environment: MYAPP_DATABASE_HOST=localhost, MYAPP_DATABASE_PORT=5432
/// 
/// var provider = new EnvironmentSettingsProvider(prefix: "MYAPP_");
/// 
/// var host = provider.Get("database.host", "default"); // Returns "localhost"
/// var port = provider.Get("database.port", 3306);      // Returns 5432
/// </code>
/// </example>
/// <seealso cref="ISettingsProvider"/>
/// <seealso cref="SharinganBuilder.UseEnvironmentVariables"/>
public class EnvironmentSettingsProvider : ISettingsProvider
{
    /// <summary>
    /// Thread-safe cache of environment variable values, keyed by normalized setting names.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional prefix for filtering environment variables.
    /// </summary>
    private readonly string? _prefix;

    /// <summary>
    /// Serializer for converting cached string values to typed values.
    /// </summary>
    private readonly ISettingsSerializer _serializer;

    /// <summary>
    /// Tracks whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <inheritdoc />
    /// <value>Returns "Environment" or "Environment:{prefix}" if a prefix was specified.</value>
    public string Name { get; }

    /// <inheritdoc />
    /// <value>Always returns <c>true</c> since environment variables cannot be written through this provider.</value>
    public bool IsReadOnly => true;

    /// <inheritdoc />
    /// <value>Returns the configured priority, defaulting to 100 (high priority for overrides).</value>
    public int Priority { get; }

    /// <inheritdoc />
    /// <value>Always returns <see cref="SettingsScope.Machine"/> since environment variables are machine-scoped.</value>
    public SettingsScope Scope => SettingsScope.Machine;

    /// <inheritdoc />
    /// <value>Returns the number of cached environment variables matching the prefix.</value>
    public int Count => _cache.Count;

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentSettingsProvider"/> class.
    /// </summary>
    /// <param name="prefix">Optional prefix for environment variable names (e.g., "MYAPP_"). Only variables starting with this prefix are included, and the prefix is stripped from the key name. If null or empty, all environment variables are included.</param>
    /// <param name="options">Optional configuration options including priority and custom serializer.</param>
    /// <param name="serializer">Optional custom serializer for type conversion. If null, uses the serializer from options or the default JSON serializer.</param>
    /// <remarks>
    /// Environment variables are loaded and cached during construction. Call <see cref="Reload"/>
    /// to refresh values if environment variables change at runtime.
    /// </remarks>
    public EnvironmentSettingsProvider(
        string? prefix = null,
        SettingsProviderOptions? options = null,
        ISettingsSerializer? serializer = null)
    {
        _prefix = prefix;
        Priority = options?.Priority ?? 100; // High priority by default
        _serializer = serializer ?? options?.Serializer ?? Serialization.JsonSettingsSerializer.Default;
        Name = string.IsNullOrEmpty(prefix) ? "Environment" : $"Environment:{prefix}";

        LoadEnvironmentVariables();
    }

    #region Synchronous Operations

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        string envKey = NormalizeKey(key);
        if (_cache.TryGetValue(envKey, out string? value))
        {
            return ConvertValue<T>(value, defaultValue);
        }

        return defaultValue;
    }

    /// <inheritdoc />
    public T? GetOrDefault<T>(string key)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        string envKey = NormalizeKey(key);
        if (_cache.TryGetValue(envKey, out string? value))
        {
            return ConvertValue<T>(value, default);
        }

        return default;
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T? value)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        string envKey = NormalizeKey(key);
        if (_cache.TryGetValue(envKey, out string? stringValue))
        {
            value = ConvertValue<T>(stringValue, default);
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public T GetOrCreate<T>(string key, Func<T> factory)
    {
        // Environment variables are read-only, so just get or return factory result
        if (TryGet<T>(key, out T? existing) && existing is not null)
        {
            return existing;
        }
        return factory();
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value)
    {
        throw new NotSupportedException("Environment variables provider is read-only.");
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        throw new NotSupportedException("Environment variables provider is read-only.");
    }

    /// <inheritdoc />
    public void Clear()
    {
        throw new NotSupportedException("Environment variables provider is read-only.");
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));
        string envKey = NormalizeKey(key);
        return _cache.ContainsKey(envKey);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllKeys()
    {
        return _cache.Keys.Select(DenormalizeKey);
    }

    /// <inheritdoc />
    public void Flush()
    {
        // No-op for environment variables
    }

    /// <inheritdoc />
    public void Reload()
    {
        _cache.Clear();
        LoadEnvironmentVariables();
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
        if (TryGet<T>(key, out T? existing) && existing is not null)
        {
            return existing;
        }
        return await factory(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Environment variables provider is read-only.");
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Environment variables provider is read-only.");
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Environment variables provider is read-only.");
    }

    /// <inheritdoc />
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Reload();
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private void LoadEnvironmentVariables()
    {
        IDictionary envVars = Environment.GetEnvironmentVariables();

        foreach (System.Collections.DictionaryEntry entry in envVars)
        {
            string? key = entry.Key?.ToString();
            string? value = entry.Value?.ToString();

            if (string.IsNullOrEmpty(key) || value is null)
            {
                continue;
            }

            // If prefix is specified, only include matching variables
            if (!string.IsNullOrEmpty(_prefix))
            {
                if (key.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
                {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    string trimmedKey = key[_prefix.Length..];
#else
                    string trimmedKey = key.Substring(_prefix.Length);
#endif
                    _cache[trimmedKey] = value;
                }
            }
            else
            {
                if (key != null)
                {
                    _cache[key] = value;
                }
            }
        }
    }

    private string NormalizeKey(string key)
    {
        // Convert dots and colons to underscores for environment variable lookup
        // e.g., "database.host" -> "DATABASE_HOST"
        return key.Replace('.', '_').Replace(':', '_').ToUpperInvariant();
    }

    private string DenormalizeKey(string envKey)
    {
        // Convert back to dot notation
        return envKey.Replace('_', '.').ToLowerInvariant();
    }

    private T? ConvertValue<T>(string value, T? defaultValue)
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        Type targetType = typeof(T);

        // Handle common primitive types directly
        if (targetType == typeof(string))
        {
            return (T)(object)value;
        }

        if (targetType == typeof(int))
        {
            return int.TryParse(value, out int intResult) ? (T)(object)intResult : defaultValue;
        }

        if (targetType == typeof(long))
        {
            return long.TryParse(value, out long longResult) ? (T)(object)longResult : defaultValue;
        }

        if (targetType == typeof(bool))
        {
            return bool.TryParse(value, out bool boolResult) ? (T)(object)boolResult : defaultValue;
        }

        if (targetType == typeof(double))
        {
            return double.TryParse(value, out double doubleResult) ? (T)(object)doubleResult : defaultValue;
        }

        if (targetType == typeof(decimal))
        {
            return decimal.TryParse(value, out decimal decimalResult) ? (T)(object)decimalResult : defaultValue;
        }

        if (targetType == typeof(Guid))
        {
            return Guid.TryParse(value, out Guid guidResult) ? (T)(object)guidResult : defaultValue;
        }

        if (targetType == typeof(TimeSpan))
        {
            return TimeSpan.TryParse(value, out TimeSpan timeSpanResult) ? (T)(object)timeSpanResult : defaultValue;
        }

        if (targetType == typeof(Uri))
        {
            return Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri? uriResult) ? (T)(object)uriResult : defaultValue;
        }

        // Try JSON deserialization for complex types
        try
        {
            return _serializer.Deserialize<T>(value) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _cache.Clear();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    #endregion
}
