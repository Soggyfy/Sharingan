using Microsoft.Win32;
using Sharingan.Abstractions;
using Sharingan.Serialization;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace Sharingan.Providers.Registry;

/// <summary>
/// A Windows Registry-based settings provider that stores settings as registry values.
/// This provider is Windows-only and provides native OS integration for settings storage.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RegistrySettingsProvider"/> stores settings in the Windows Registry,
/// offering several advantages for Windows applications:
/// <list type="bullet">
/// <item><description>Native OS integration with Windows backup and restore</description></item>
/// <item><description>Immediate persistence (no flush required)</description></item>
/// <item><description>ACL-based security for protected settings</description></item>
/// <item><description>Familiar location for Windows administrators</description></item>
/// </list>
/// </para>
/// <para>
/// Settings are stored under a configurable registry path, typically:
/// <c>HKEY_CURRENT_USER\Software\{Organization}\{Application}</c> for user scope, or
/// <c>HKEY_LOCAL_MACHINE\Software\{Organization}\{Application}</c> for machine scope.
/// </para>
/// <para>
/// <strong>Platform Note:</strong> This provider is Windows-only. It will throw
/// <see cref="PlatformNotSupportedException"/> on Linux, macOS, or other platforms.
/// </para>
/// </remarks>
/// <example>
/// Using the Registry provider:
/// <code>
/// var provider = new RegistrySettingsProvider(new RegistryProviderOptions
/// {
///     Scope = SettingsScope.User,
///     SubKeyPath = @"Software\MyCompany\MyApp"
/// });
/// 
/// // Value is immediately written to registry
/// provider.Set("theme", "dark");
/// 
/// // Read from registry
/// var theme = provider.Get("theme", "light");
/// </code>
/// </example>
/// <seealso cref="RegistryProviderOptions"/>
/// <seealso cref="SharinganBuilderExtensions.UseRegistry"/>
#if NET5_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
public class RegistrySettingsProvider : ISettingsProvider
{
    /// <summary>
    /// Configuration options for this provider.
    /// </summary>
    private readonly RegistryProviderOptions _options;

    /// <summary>
    /// Serializer for converting complex types to registry-compatible strings.
    /// </summary>
    private readonly ISettingsSerializer _serializer;

    /// <summary>
    /// The root registry key (e.g., HKEY_CURRENT_USER or HKEY_LOCAL_MACHINE).
    /// </summary>
    private readonly RegistryKey _rootKey;

    /// <summary>
    /// The sub-key path under the root key where settings are stored.
    /// </summary>
    private readonly string _subKeyPath;

    /// <summary>
    /// Tracks whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <inheritdoc />
    /// <value>Returns "Registry:{subKeyPath}" for identification.</value>
    public string Name => $"Registry:{_subKeyPath}";

    /// <inheritdoc />
    /// <value>Always returns <c>false</c> since registry values are writable (permissions permitting).</value>
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public int Priority => _options.Priority;

    /// <inheritdoc />
    public SettingsScope Scope => _options.Scope;

    /// <inheritdoc />
    public int Count => GetAllKeys().Count();

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrySettingsProvider"/> class.
    /// </summary>
    /// <param name="options">Configuration options for registry hive, sub-key path, and scope. If null, default options are used.</param>
    /// <param name="serializer">Optional custom serializer for complex type conversion. If null, uses the default JSON serializer.</param>
    /// <exception cref="PlatformNotSupportedException">Thrown when running on non-Windows platforms.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller lacks permission to access the registry key.</exception>
    public RegistrySettingsProvider(RegistryProviderOptions? options = null, ISettingsSerializer? serializer = null)
    {
        _options = options ?? new RegistryProviderOptions();
        _serializer = serializer ?? _options.Serializer ?? JsonSettingsSerializer.Default;

        RegistryHive hive = _options.Hive ?? (_options.Scope == SettingsScope.Machine ? RegistryHive.LocalMachine : RegistryHive.CurrentUser);
        _rootKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);

        _subKeyPath = _options.SubKeyPath ?? BuildSubKeyPath();

        if (_options.CreateIfNotExists)
        {
            EnsureKeyExists();
        }
    }

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        using RegistryKey? subKey = _rootKey.OpenSubKey(_subKeyPath);
        if (subKey is null)
        {
            return defaultValue;
        }

        object? value = subKey.GetValue(key);
        if (value is null)
        {
            return defaultValue;
        }

        return ConvertValue<T>(value, defaultValue);
    }

    /// <inheritdoc />
    public T? GetOrDefault<T>(string key)
    {
        using RegistryKey? subKey = _rootKey.OpenSubKey(_subKeyPath);
        if (subKey is null)
        {
            return default;
        }

        object? value = subKey.GetValue(key);
        if (value is null)
        {
            return default;
        }

        return ConvertValue<T>(value, default);
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T? value)
    {
        using RegistryKey? subKey = _rootKey.OpenSubKey(_subKeyPath);
        if (subKey is null)
        {
            value = default;
            return false;
        }

        object? regValue = subKey.GetValue(key);
        if (regValue is null)
        {
            value = default;
            return false;
        }

        value = ConvertValue<T>(regValue, default);
        return true;
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
        using RegistryKey subKey = _rootKey.CreateSubKey(_subKeyPath);

        // Store strings directly without JSON serialization
        string stringValue = value is string s ? s : _serializer.Serialize(value);
        subKey.SetValue(key, stringValue, RegistryValueKind.String);

        OnSettingsChanged(new SettingsChangedEventArgs(key, SettingsChangeType.Modified, null, value, Name));
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        using RegistryKey? subKey = _rootKey.OpenSubKey(_subKeyPath, writable: true);
        if (subKey is null)
        {
            return false;
        }

        try
        {
            subKey.DeleteValue(key, throwOnMissingValue: false);
            OnSettingsChanged(new SettingsChangedEventArgs(key, SettingsChangeType.Removed, null, null, Name));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        using RegistryKey? subKey = _rootKey.OpenSubKey(_subKeyPath, writable: true);
        if (subKey is null)
        {
            return;
        }

        foreach (string valueName in subKey.GetValueNames())
        {
            subKey.DeleteValue(valueName, throwOnMissingValue: false);
        }

        OnSettingsChanged(SettingsChangedEventArgs.Cleared(Name));
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        using RegistryKey? subKey = _rootKey.OpenSubKey(_subKeyPath);
        return subKey?.GetValue(key) is not null;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllKeys()
    {
        using RegistryKey? subKey = _rootKey.OpenSubKey(_subKeyPath);
        return subKey?.GetValueNames() ?? [];
    }

    /// <inheritdoc />
    public void Flush() { /* Registry writes are immediate */ }

    /// <inheritdoc />
    public void Reload() { /* Nothing to reload */ }

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken ct = default)
    {
        return Task.FromResult(Get(key, defaultValue));
    }

    /// <inheritdoc />
    public Task<T?> GetOrDefaultAsync<T>(string key, CancellationToken ct = default)
    {
        return Task.FromResult(GetOrDefault<T>(key));
    }

    /// <inheritdoc />
    public Task<T> GetOrCreateAsync<T>(string key, Func<T> factory, CancellationToken ct = default)
    {
        return Task.FromResult(GetOrCreate(key, factory));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, CancellationToken ct = default) { Set(key, value); return Task.CompletedTask; }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(Remove(key));
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken ct = default) { Clear(); return Task.CompletedTask; }

    /// <inheritdoc />
    public Task FlushAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReloadAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    private string BuildSubKeyPath()
    {
        string org = _options.OrganizationName ?? "Sharingan";
        string app = _options.ApplicationName ?? "Settings";
        return $@"Software\{org}\{app}";
    }

    private void EnsureKeyExists()
    {
        using RegistryKey subKey = _rootKey.CreateSubKey(_subKeyPath);
    }

    private T? ConvertValue<T>(object value, T? defaultValue)
    {
        if (value is T typed)
        {
            return typed;
        }

        if (value is string str)
        {
            return _serializer.Deserialize<T>(str) ?? defaultValue;
        }

        return defaultValue;
    }

    protected virtual void OnSettingsChanged(SettingsChangedEventArgs e)
    {
        SettingsChanged?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _rootKey.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
