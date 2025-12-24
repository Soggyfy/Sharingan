using Sharingan.Abstractions;
using Sharingan.Internal;
using System.Collections.Concurrent;

namespace Sharingan.Providers;

/// <summary>
/// An in-memory settings provider for session-scoped, non-persistent storage.
/// All settings are stored in RAM and lost when the application terminates.
/// Thread-safe implementation using <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="InMemorySettingsProvider"/> is ideal for:
/// <list type="bullet">
/// <item><description>Session-scoped settings that should not persist across application restarts</description></item>
/// <item><description>Temporary runtime overrides</description></item>
/// <item><description>Testing and mocking scenarios where file I/O should be avoided</description></item>
/// <item><description>Caching computed or derived configuration values</description></item>
/// <item><description>Fast, zero-latency settings access</description></item>
/// </list>
/// </para>
/// <para>
/// This provider is fully thread-safe and supports concurrent access from multiple threads.
/// All operations are O(1) complexity.
/// </para>
/// <para>
/// The <see cref="ISettingsStore.Flush"/> and <see cref="ISettingsStore.FlushAsync"/>
/// methods are no-ops since there is no persistent storage to write to.
/// </para>
/// </remarks>
/// <example>
/// Using in-memory provider for session settings:
/// <code>
/// var sessionStore = new InMemorySettingsProvider("Session");
/// 
/// // Store session-specific data
/// sessionStore.Set("session.startTime", DateTime.UtcNow);
/// sessionStore.Set("session.userId", currentUser.Id);
/// 
/// // In composite scenarios
/// var store = new SharinganBuilder()
///     .UseInMemory("Overrides", priority: 100)  // Highest priority for runtime overrides
///     .UseJsonFile("settings.json", priority: 50)
///     .Build();
/// </code>
/// </example>
/// <seealso cref="ISettingsProvider"/>
/// <seealso cref="SharinganBuilder.UseInMemory"/>
public class InMemorySettingsProvider : ISettingsProvider
{
    /// <summary>
    /// Thread-safe dictionary storing all settings as key-value pairs.
    /// </summary>
    private readonly ConcurrentDictionary<string, object?> _settings = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Serializer for type conversion when needed.
    /// </summary>
    private readonly ISettingsSerializer _serializer;

    /// <summary>
    /// Tracks whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <inheritdoc />
    /// <value>Returns the configured name, defaulting to "InMemory".</value>
    public string Name { get; }

    /// <inheritdoc />
    /// <value>Always returns <c>false</c> since in-memory storage is always writable.</value>
    public bool IsReadOnly => false;

    /// <inheritdoc />
    /// <value>Returns the configured priority, defaulting to 0.</value>
    public int Priority { get; }

    /// <inheritdoc />
    /// <value>Always returns <see cref="SettingsScope.Session"/> since in-memory data is session-scoped.</value>
    public SettingsScope Scope => SettingsScope.Session;

    /// <inheritdoc />
    /// <value>Returns the current number of settings stored in memory.</value>
    public int Count => _settings.Count;

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemorySettingsProvider"/> class.
    /// </summary>
    /// <param name="name">The unique name for this provider, used for identification in composite scenarios. Defaults to "InMemory".</param>
    /// <param name="options">Optional configuration options including priority and custom serializer.</param>
    /// <param name="serializer">Optional custom serializer for type conversion. If null, uses the serializer from options or the default JSON serializer.</param>
    public InMemorySettingsProvider(
        string? name = null,
        SettingsProviderOptions? options = null,
        ISettingsSerializer? serializer = null)
    {
        Name = name ?? "InMemory";
        Priority = options?.Priority ?? 0;
        _serializer = serializer ?? options?.Serializer ?? Serialization.JsonSettingsSerializer.Default;
    }

    #region Synchronous Operations

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        if (_settings.TryGetValue(key, out object? value) && value is not null)
        {
            return ConvertValue<T>(value);
        }

        return defaultValue;
    }

    /// <inheritdoc />
    public T? GetOrDefault<T>(string key)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        if (_settings.TryGetValue(key, out object? value) && value is not null)
        {
            return ConvertValue<T>(value);
        }

        return default;
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T? value)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        if (_settings.TryGetValue(key, out object? stored) && stored is not null)
        {
            value = ConvertValue<T>(stored);
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public T GetOrCreate<T>(string key, Func<T> factory)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));
        ThrowHelper.ThrowIfNull(factory, nameof(factory));

        if (_settings.TryGetValue(key, out object? existing) && existing is not null)
        {
            return ConvertValue<T>(existing);
        }

        T? value = factory();
        Set(key, value);
        return value;
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        object? oldValue = _settings.TryGetValue(key, out object? existing) ? existing : null;
        bool isNew = oldValue is null;

        _settings[key] = value;

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

        if (_settings.TryRemove(key, out object? oldValue))
        {
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
        _settings.Clear();
        OnSettingsChanged(SettingsChangedEventArgs.Cleared(Name));
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));
        return _settings.ContainsKey(key);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllKeys()
    {
        return _settings.Keys;
    }

    /// <inheritdoc />
    public void Flush()
    {
        // No-op for in-memory provider
    }

    /// <inheritdoc />
    public void Reload()
    {
        // No-op for in-memory provider
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

        if (_settings.TryGetValue(key, out object? existing) && existing is not null)
        {
            return ConvertValue<T>(existing);
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
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    #endregion

    /// <summary>
    /// Raises the <see cref="SettingsChanged"/> event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnSettingsChanged(SettingsChangedEventArgs e)
    {
        SettingsChanged?.Invoke(this, e);
    }

    private T ConvertValue<T>(object value)
    {
        if (value is T typed)
        {
            return typed;
        }

        // Try to convert through serialization
        if (value is string stringValue)
        {
            T? result = _serializer.Deserialize<T>(stringValue);
            return result ?? throw new InvalidOperationException($"Failed to deserialize value for type {typeof(T)}");
        }

        // Serialize and deserialize for type conversion
        string serialized = _serializer.Serialize(value);
        T? deserialized = _serializer.Deserialize<T>(serialized);
        return deserialized ?? throw new InvalidOperationException($"Failed to convert value to type {typeof(T)}");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _settings.Clear();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
