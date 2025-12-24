namespace Sharingan.Abstractions;

/// <summary>
/// Represents a pluggable settings provider that can be used as a backend for settings storage.
/// Providers can be chained together in a composite store for layered configuration with
/// priority-based resolution.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ISettingsProvider"/> interface extends <see cref="ISettingsStore"/> with
/// additional capabilities required for use as a pluggable storage backend. Providers are
/// designed to be composable, allowing multiple providers to be chained together with
/// priority-based value resolution.
/// </para>
/// <para>
/// Each provider has a unique <see cref="Name"/> for identification, a <see cref="Priority"/>
/// that determines the order in which providers are consulted when reading values, and a
/// <see cref="Scope"/> that indicates the logical scope of the settings (user, machine, etc.).
/// </para>
/// <para>
/// Providers support change notifications through the <see cref="SettingsChanged"/> event,
/// enabling reactive programming patterns and automatic UI updates when settings are modified
/// externally.
/// </para>
/// <para>
/// Implementations should be thread-safe and handle concurrent access appropriately.
/// The provider should properly dispose of any resources when <see cref="IDisposable.Dispose"/>
/// is called.
/// </para>
/// </remarks>
/// <example>
/// Creating a composite configuration with multiple providers:
/// <code>
/// var settings = new SharinganBuilder()
///     .UseEnvironmentVariables(prefix: "MYAPP_", priority: 100)  // Highest priority
///     .UseJsonFile("settings.json", SettingsScope.User, priority: 50)
///     .UseJsonFile("defaults.json", SettingsScope.Application, priority: 0)
///     .Build();
/// 
/// // Environment variables will be checked first, then user settings, then defaults
/// var connectionString = settings.Get("database.connection", "localhost");
/// </code>
/// </example>
/// <seealso cref="ISettingsStore"/>
/// <seealso cref="SettingsScope"/>
/// <seealso cref="SettingsChangedEventArgs"/>
public interface ISettingsProvider : ISettingsStore, IDisposable
{
    /// <summary>
    /// Gets the unique name of this provider, used for identification and logging purposes.
    /// </summary>
    /// <value>
    /// A string that uniquely identifies this provider instance. The format typically includes
    /// the provider type and relevant configuration details (e.g., "JsonFile:settings.json",
    /// "Environment:MYAPP_", "Registry:HKCU\Software\MyApp").
    /// </value>
    /// <remarks>
    /// The name is used primarily for debugging, logging, and identifying the source of
    /// settings in composite provider scenarios. It should be descriptive enough to distinguish
    /// between multiple instances of the same provider type.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether this provider is read-only.
    /// </summary>
    /// <value>
    /// <c>true</c> if the provider does not support write operations (Set, Remove, Clear);
    /// <c>false</c> if the provider supports both read and write operations.
    /// </value>
    /// <remarks>
    /// <para>
    /// Read-only providers (such as environment variables) will throw
    /// <see cref="NotSupportedException"/> when write operations are attempted.
    /// </para>
    /// <para>
    /// In composite provider scenarios, write operations are directed to the first
    /// writable provider (or a designated write target), skipping read-only providers.
    /// </para>
    /// </remarks>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets the priority of this provider for composite/chained scenarios.
    /// </summary>
    /// <value>
    /// An integer representing the provider's priority. Higher values indicate higher priority
    /// (checked first when reading values). Default priority is typically 0.
    /// </value>
    /// <remarks>
    /// <para>
    /// When multiple providers are combined in a composite store, the provider with the
    /// highest priority is consulted first when reading a value. If the value is not found,
    /// subsequent providers are checked in descending priority order.
    /// </para>
    /// <para>
    /// Common priority conventions:
    /// <list type="bullet">
    /// <item><description>100+: Environment variables and command-line overrides</description></item>
    /// <item><description>50-99: User-specific settings</description></item>
    /// <item><description>0-49: Application defaults and fallback values</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    int Priority { get; }

    /// <summary>
    /// Gets the settings scope that this provider is configured for.
    /// </summary>
    /// <value>
    /// A <see cref="SettingsScope"/> value indicating the logical scope of settings
    /// managed by this provider (e.g., User, Machine, Application, Session).
    /// </value>
    /// <remarks>
    /// The scope determines the storage location and accessibility of settings.
    /// For example, user-scoped settings are stored per-user and not shared between
    /// users on the same machine, while machine-scoped settings are shared.
    /// </remarks>
    /// <seealso cref="SettingsScope"/>
    SettingsScope Scope { get; }

    /// <summary>
    /// Reloads settings from the underlying storage, refreshing the in-memory cache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method discards any cached values and re-reads all settings from the
    /// underlying storage source. This is useful when settings may have been modified
    /// externally (e.g., by another process or through direct file editing).
    /// </para>
    /// <para>
    /// For providers that do not cache values (e.g., registry provider), this method
    /// may be a no-op.
    /// </para>
    /// <para>
    /// Calling <see cref="Reload"/> may trigger <see cref="SettingsChanged"/> events
    /// if values have changed since the last load.
    /// </para>
    /// </remarks>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading from the underlying storage.</exception>
    void Reload();

    /// <summary>
    /// Asynchronously reloads settings from the underlying storage, refreshing the in-memory cache.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the reload operation.</param>
    /// <returns>A task representing the asynchronous reload operation.</returns>
    /// <remarks>
    /// <para>
    /// This method discards any cached values and re-reads all settings from the
    /// underlying storage source. This is useful when settings may have been modified
    /// externally (e.g., by another process or through direct file editing).
    /// </para>
    /// <para>
    /// For providers that do not cache values (e.g., registry provider), this method
    /// may complete immediately.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading from the underlying storage.</exception>
    Task ReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Occurs when a setting value changes in this provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised when a setting is added, modified, removed, or when all settings
    /// are cleared. The <see cref="SettingsChangedEventArgs"/> provides details about the
    /// specific change, including the key, old and new values, and the type of change.
    /// </para>
    /// <para>
    /// For file-based providers with file watching enabled, this event may also be raised
    /// when external changes to the settings file are detected.
    /// </para>
    /// <para>
    /// Event handlers should be lightweight and not perform long-running operations, as
    /// they may be invoked synchronously from the thread that made the change.
    /// </para>
    /// </remarks>
    /// <seealso cref="SettingsChangedEventArgs"/>
    /// <seealso cref="SettingsChangeType"/>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
}
