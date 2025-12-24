using Sharingan.Abstractions;
using Sharingan.Internal;

namespace Sharingan.Providers;

/// <summary>
/// A composite settings provider that chains multiple providers together with priority-based
/// value resolution. Reads check providers in priority order (highest first) until a value
/// is found. Writes go to the first writable provider or a designated write target.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CompositeSettingsProvider"/> enables layered configuration patterns where
/// multiple settings sources are combined with clear precedence rules. Common scenarios include:
/// <list type="bullet">
/// <item><description>Environment variables overriding file-based settings</description></item>
/// <item><description>User settings overriding application defaults</description></item>
/// <item><description>Machine-wide settings providing fallback values</description></item>
/// </list>
/// </para>
/// <para>
/// Read operations iterate through providers in descending priority order and return the first
/// value found. Write operations are directed to either a designated write target or the first
/// writable provider in priority order.
/// </para>
/// <para>
/// This provider subscribes to <see cref="ISettingsProvider.SettingsChanged"/> events from all
/// child providers and re-raises them, enabling unified change notifications across all sources.
/// </para>
/// </remarks>
/// <example>
/// Creating a composite provider with multiple sources:
/// <code>
/// var envProvider = new EnvironmentSettingsProvider("MYAPP_", priority: 100);
/// var userSettings = new JsonFileSettingsProvider(new JsonFileProviderOptions 
/// { 
///     FilePath = "user.json", 
///     Priority = 50 
/// });
/// var defaults = new JsonFileSettingsProvider(new JsonFileProviderOptions 
/// { 
///     FilePath = "defaults.json", 
///     Priority = 0 
/// });
/// 
/// var composite = new CompositeSettingsProvider(
///     [envProvider, userSettings, defaults], 
///     writeTarget: userSettings);
/// 
/// // Reads check: envProvider (100) → userSettings (50) → defaults (0)
/// // Writes go to: userSettings
/// </code>
/// </example>
/// <seealso cref="ISettingsProvider"/>
/// <seealso cref="SharinganBuilder"/>
public class CompositeSettingsProvider : ISettingsProvider
{
    /// <summary>
    /// The list of providers ordered by descending priority (highest first).
    /// </summary>
    private readonly List<ISettingsProvider> _providers;

    /// <summary>
    /// The designated provider for write operations, or null to use first writable provider.
    /// </summary>
    private readonly ISettingsProvider? _writeTarget;

    /// <summary>
    /// Tracks whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <inheritdoc />
    /// <value>Always returns "Composite" to identify this as a composite provider.</value>
    public string Name => "Composite";

    /// <inheritdoc />
    /// <value>
    /// <c>true</c> if the write target is read-only (when specified) or all providers are read-only;
    /// <c>false</c> if at least one writable provider exists.
    /// </value>
    public bool IsReadOnly => _writeTarget?.IsReadOnly ?? _providers.All(p => p.IsReadOnly);

    /// <inheritdoc />
    /// <value>Returns the maximum priority value among all child providers.</value>
    public int Priority => _providers.Max(p => p.Priority);

    /// <inheritdoc />
    /// <value>Returns the scope of the write target, or the first provider's scope, or User as default.</value>
    public SettingsScope Scope => _writeTarget?.Scope ?? _providers.FirstOrDefault()?.Scope ?? SettingsScope.User;

    /// <inheritdoc />
    /// <value>Returns the count of unique keys across all providers (case-insensitive).</value>
    public int Count => _providers.SelectMany(p => p.GetAllKeys()).Distinct(StringComparer.OrdinalIgnoreCase).Count();

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeSettingsProvider"/> class
    /// with the specified providers and optional write target.
    /// </summary>
    /// <param name="providers">The providers to chain together. Must contain at least one provider. Providers are automatically ordered by priority (highest first).</param>
    /// <param name="writeTarget">Optional specific provider for write operations. If null, the first writable provider in priority order is used for writes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="providers"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="providers"/> is empty.</exception>
    /// <remarks>
    /// The constructor automatically subscribes to <see cref="ISettingsProvider.SettingsChanged"/>
    /// events on all child providers to enable unified change notification.
    /// </remarks>
    public CompositeSettingsProvider(IEnumerable<ISettingsProvider> providers, ISettingsProvider? writeTarget = null)
    {
        ThrowHelper.ThrowIfNull(providers, nameof(providers));

        _providers = [.. providers.OrderByDescending(p => p.Priority)];
        _writeTarget = writeTarget;

        if (_providers.Count == 0)
        {
            throw new ArgumentException("At least one provider is required.", nameof(providers));
        }

        // Subscribe to child provider events
        foreach (ISettingsProvider provider in _providers)
        {
            provider.SettingsChanged += OnChildSettingsChanged;
        }
    }

    #region Synchronous Operations

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        foreach (ISettingsProvider provider in _providers)
        {
            if (provider.TryGet<T>(key, out T? value) && value is not null)
            {
                return value;
            }
        }

        return defaultValue;
    }

    /// <inheritdoc />
    public T? GetOrDefault<T>(string key)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        foreach (ISettingsProvider provider in _providers)
        {
            if (provider.TryGet<T>(key, out T? value) && value is not null)
            {
                return value;
            }
        }

        return default;
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T? value)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        foreach (ISettingsProvider provider in _providers)
        {
            if (provider.TryGet<T>(key, out value))
            {
                return true;
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
        ISettingsProvider target = GetWriteTarget();
        target.Set(key, value);
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        ISettingsProvider target = GetWriteTarget();
        return target.Remove(key);
    }

    /// <inheritdoc />
    public void Clear()
    {
        ISettingsProvider target = GetWriteTarget();
        target.Clear();
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));
        return _providers.Any(p => p.ContainsKey(key));
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllKeys()
    {
        return _providers
            .SelectMany(p => p.GetAllKeys())
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public void Flush()
    {
        foreach (ISettingsProvider provider in _providers)
        {
            provider.Flush();
        }
    }

    /// <inheritdoc />
    public void Reload()
    {
        foreach (ISettingsProvider provider in _providers)
        {
            provider.Reload();
        }
    }

    #endregion

    #region Asynchronous Operations

    /// <inheritdoc />
    public async Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        foreach (ISettingsProvider provider in _providers)
        {
            T? value = await provider.GetOrDefaultAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (value is not null)
            {
                return value;
            }
        }

        return defaultValue;
    }

    /// <inheritdoc />
    public async Task<T?> GetOrDefaultAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));

        foreach (ISettingsProvider provider in _providers)
        {
            T? value = await provider.GetOrDefaultAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (value is not null)
            {
                return value;
            }
        }

        return default;
    }

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(string key, Func<T> factory, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));
        ThrowHelper.ThrowIfNull(factory, nameof(factory));

        T? existing = await GetOrDefaultAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        T? value = factory();
        await SetAsync(key, value, cancellationToken).ConfigureAwait(false);
        return value;
    }

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));
        ThrowHelper.ThrowIfNull(factory, nameof(factory));

        T? existing = await GetOrDefaultAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        T? value = await factory(cancellationToken).ConfigureAwait(false);
        await SetAsync(key, value, cancellationToken).ConfigureAwait(false);
        return value;
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        ISettingsProvider target = GetWriteTarget();
        return target.SetAsync(key, value, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ISettingsProvider target = GetWriteTarget();
        return target.RemoveAsync(key, cancellationToken);
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        ISettingsProvider target = GetWriteTarget();
        return target.ClearAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        foreach (ISettingsProvider provider in _providers)
        {
            await provider.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        foreach (ISettingsProvider provider in _providers)
        {
            await provider.ReloadAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets the provider to use for write operations.
    /// </summary>
    /// <returns>The write target provider.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the write target is read-only or no writable provider exists.</exception>
    private ISettingsProvider GetWriteTarget()
    {
        if (_writeTarget is not null)
        {
            if (_writeTarget.IsReadOnly)
            {
                throw new InvalidOperationException($"Write target '{_writeTarget.Name}' is read-only.");
            }
            return _writeTarget;
        }

        ISettingsProvider? writableProvider = _providers.FirstOrDefault(p => !p.IsReadOnly);
        if (writableProvider is null)
        {
            throw new InvalidOperationException("No writable provider available in the composite.");
        }

        return writableProvider;
    }

    /// <summary>
    /// Handles settings changed events from child providers by re-raising them.
    /// </summary>
    /// <param name="sender">The child provider that raised the event.</param>
    /// <param name="e">The event arguments containing change details.</param>
    private void OnChildSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        SettingsChanged?.Invoke(this, e);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (ISettingsProvider provider in _providers)
            {
                provider.SettingsChanged -= OnChildSettingsChanged;
                provider.Dispose();
            }
            _providers.Clear();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    #endregion
}
