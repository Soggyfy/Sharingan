using Sharingan.Abstractions;
using Sharingan.Internal;
using Sharingan.Providers;

namespace Sharingan;

/// <summary>
/// Fluent builder for configuring and constructing Sharingan settings stores.
/// Provides a chainable API for adding providers, setting options, and building
/// composite settings stores with priority-based value resolution.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SharinganBuilder"/> class is the primary entry point for configuring
/// custom settings stores. It supports:
/// <list type="bullet">
/// <item><description>Multiple providers with priority-based resolution</description></item>
/// <item><description>Different storage formats (JSON, INI, YAML, XML, TOML, SQLite, Registry)</description></item>
/// <item><description>Environment variable overlays for deployment configuration</description></item>
/// <item><description>In-memory providers for testing and session-scoped data</description></item>
/// <item><description>Custom serializers for specialized data formats</description></item>
/// <item><description>Encryption wrappers for sensitive data</description></item>
/// </list>
/// </para>
/// <para>
/// When multiple providers are configured, they are combined into a composite store.
/// Read operations check providers in descending priority order (highest first) until
/// a value is found. Write operations are directed to the first writable provider or
/// a designated write target.
/// </para>
/// </remarks>
/// <example>
/// Basic usage with a single JSON file:
/// <code>
/// var store = new SharinganBuilder()
///     .WithApplicationName("MyApp")
///     .UseJsonFile("settings.json", SettingsScope.User)
///     .Build();
/// </code>
/// </example>
/// <example>
/// Advanced usage with multiple providers:
/// <code>
/// var store = new SharinganBuilder()
///     .WithApplicationName("MyApp")
///     .WithOrganizationName("MyCompany")
///     .UseEnvironmentVariables(prefix: "MYAPP_", priority: 100)  // Highest priority
///     .UseJsonFile("user-settings.json", SettingsScope.User, priority: 50)
///     .UseJsonFile("defaults.json", SettingsScope.Application, priority: 0)
///     .Build();
/// 
/// // Environment variables are checked first, then user settings, then defaults
/// var theme = store.Get("ui.theme", "light");
/// </code>
/// </example>
/// <seealso cref="ISettingsStore"/>
/// <seealso cref="ISettingsProvider"/>
/// <seealso cref="Settings"/>
public class SharinganBuilder
{
    /// <summary>
    /// Collection of providers that will be combined into the final settings store.
    /// Providers are added in the order of method calls and later sorted by priority.
    /// </summary>
    private readonly List<ISettingsProvider> _providers = [];

    /// <summary>
    /// Optional designated provider for write operations. If null, the first writable
    /// provider (in priority order) is used for writes.
    /// </summary>
    private ISettingsProvider? _writeTarget;

    /// <summary>
    /// Optional custom serializer to use for all providers that don't specify their own.
    /// </summary>
    private ISettingsSerializer? _serializer;

    /// <summary>
    /// Application name used for path resolution in file-based providers.
    /// </summary>
    private string? _applicationName;

    /// <summary>
    /// Organization name used for path resolution in file-based providers.
    /// </summary>
    private string? _organizationName;

    /// <summary>
    /// Sets the application name used for path resolution when storing settings files.
    /// The application name becomes part of the directory path for settings storage.
    /// </summary>
    /// <param name="applicationName">The application name to use. Cannot be null, empty, or whitespace.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is null, empty, or consists only of whitespace.</exception>
    /// <remarks>
    /// The application name is combined with the scope and optional organization name to determine
    /// the full path for settings storage. For example, on Windows with user scope:
    /// <c>%APPDATA%\{OrganizationName}\{ApplicationName}\settings.json</c>
    /// </remarks>
    /// <example>
    /// <code>
    /// var store = new SharinganBuilder()
    ///     .WithApplicationName("MyAwesomeApp")
    ///     .UseJsonFile()
    ///     .Build();
    /// </code>
    /// </example>
    public SharinganBuilder WithApplicationName(string applicationName)
    {
        _applicationName = ThrowHelper.ThrowIfNullOrWhiteSpace(applicationName, nameof(applicationName));
        return this;
    }

    /// <summary>
    /// Sets the organization name used for path resolution when storing settings files.
    /// The organization name creates an additional directory level in the settings path.
    /// </summary>
    /// <param name="organizationName">The organization name to use. Cannot be null, empty, or whitespace.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="organizationName"/> is null, empty, or consists only of whitespace.</exception>
    /// <remarks>
    /// <para>
    /// The organization name is useful for grouping multiple applications from the same
    /// organization under a common parent directory.
    /// </para>
    /// <para>
    /// Example paths:
    /// <list type="bullet">
    /// <item><description>With organization: <c>%APPDATA%\MyCompany\MyApp\settings.json</c></description></item>
    /// <item><description>Without organization: <c>%APPDATA%\MyApp\settings.json</c></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public SharinganBuilder WithOrganizationName(string organizationName)
    {
        _organizationName = ThrowHelper.ThrowIfNullOrWhiteSpace(organizationName, nameof(organizationName));
        return this;
    }

    /// <summary>
    /// Sets a custom serializer to use for all providers that don't specify their own serializer.
    /// The serializer is responsible for converting between typed objects and their string representation.
    /// </summary>
    /// <param name="serializer">The serializer implementation to use. Cannot be null.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serializer"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// The default serializer uses System.Text.Json with the following settings:
    /// <list type="bullet">
    /// <item><description>Pretty-printed output (WriteIndented = true)</description></item>
    /// <item><description>CamelCase property naming</description></item>
    /// <item><description>Null values ignored when writing</description></item>
    /// <item><description>Comments allowed when reading</description></item>
    /// <item><description>Trailing commas allowed</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Custom serializers can be used to support alternative formats, apply encryption,
    /// or handle special types that require custom conversion logic.
    /// </para>
    /// </remarks>
    public SharinganBuilder WithSerializer(ISettingsSerializer serializer)
    {
        _serializer = ThrowHelper.ThrowIfNull(serializer, nameof(serializer));
        return this;
    }

    /// <summary>
    /// Adds a JSON file provider to the settings store configuration.
    /// </summary>
    /// <param name="filePath">The path to the JSON file. Can be relative (resolved based on scope and application name) or absolute. Default is "settings.json".</param>
    /// <param name="scope">The settings scope determining where the file is stored. Default is <see cref="SettingsScope.User"/>.</param>
    /// <param name="priority">The provider priority for composite stores. Higher values are checked first when reading. Default is 0.</param>
    /// <param name="configure">Optional action to configure additional provider options such as file watching, retry settings, and atomic writes.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// The JSON file provider features:
    /// <list type="bullet">
    /// <item><description>Atomic writes (write to temp file, then rename) to prevent corruption</description></item>
    /// <item><description>File watching for external change detection (optional)</description></item>
    /// <item><description>Automatic file/directory creation if not exists</description></item>
    /// <item><description>Retry logic for handling file lock contention</description></item>
    /// <item><description>In-memory caching with explicit flush</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var store = new SharinganBuilder()
    ///     .UseJsonFile("config.json", SettingsScope.User, priority: 50, configure: options =>
    ///     {
    ///         options.WatchForChanges = true;
    ///         options.UseAtomicWrites = true;
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    public SharinganBuilder UseJsonFile(
        string filePath = "settings.json",
        SettingsScope scope = SettingsScope.User,
        int priority = 0,
        Action<JsonFileProviderOptions>? configure = null)
    {
        JsonFileProviderOptions options = new()
        {
            FilePath = filePath,
            Scope = scope,
            Priority = priority,
            ApplicationName = _applicationName,
            OrganizationName = _organizationName,
            Serializer = _serializer
        };

        configure?.Invoke(options);

        JsonFileSettingsProvider provider = new(options, _serializer);
        _providers.Add(provider);

        return this;
    }

    /// <summary>
    /// Adds environment variables as a read-only provider to the settings store configuration.
    /// Environment variables provide the highest priority settings, ideal for deployment-time configuration.
    /// </summary>
    /// <param name="prefix">Optional prefix for environment variable names. Only variables starting with this prefix are included, and the prefix is stripped from the key. For example, with prefix "MYAPP_", the variable "MYAPP_DATABASE_HOST" becomes key "database.host".</param>
    /// <param name="priority">The provider priority. Default is 100 (high priority) since environment variables typically override file-based settings.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// The environment variables provider:
    /// <list type="bullet">
    /// <item><description>Is always read-only (write operations throw <see cref="NotSupportedException"/>)</description></item>
    /// <item><description>Converts underscores to dots for key lookup (e.g., DATABASE_HOST → database.host)</description></item>
    /// <item><description>Performs case-insensitive key matching</description></item>
    /// <item><description>Caches values at construction time (call Reload to refresh)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This provider is particularly useful for:
    /// <list type="bullet">
    /// <item><description>Docker/container deployments</description></item>
    /// <item><description>CI/CD pipelines</description></item>
    /// <item><description>Twelve-factor app configuration</description></item>
    /// <item><description>Secret injection via orchestration platforms</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var store = new SharinganBuilder()
    ///     .UseEnvironmentVariables(prefix: "MYAPP_", priority: 100)
    ///     .UseJsonFile("settings.json", priority: 0)
    ///     .Build();
    /// 
    /// // Set environment variable: MYAPP_DATABASE_HOST=localhost
    /// var host = store.Get("database.host", "default"); // Returns "localhost"
    /// </code>
    /// </example>
    public SharinganBuilder UseEnvironmentVariables(string? prefix = null, int priority = 100)
    {
        SettingsProviderOptions options = new()
        {
            Priority = priority,
            IsReadOnly = true,
            Serializer = _serializer
        };

        EnvironmentSettingsProvider provider = new(prefix, options, _serializer);
        _providers.Add(provider);

        return this;
    }

    /// <summary>
    /// Adds an in-memory provider to the settings store configuration.
    /// In-memory providers store settings in RAM only and are not persisted to disk.
    /// </summary>
    /// <param name="name">Optional unique name for this provider. Used for identification in composite stores. Default is "InMemory".</param>
    /// <param name="priority">The provider priority. Default is 50.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// The in-memory provider is useful for:
    /// <list type="bullet">
    /// <item><description>Session-scoped settings that should not persist across application restarts</description></item>
    /// <item><description>Temporary overrides during runtime</description></item>
    /// <item><description>Testing and mocking scenarios</description></item>
    /// <item><description>Caching computed configuration values</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The provider is thread-safe using <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var store = new SharinganBuilder()
    ///     .UseInMemory("SessionSettings", priority: 75)
    ///     .UseJsonFile("settings.json", priority: 50)
    ///     .Build();
    /// 
    /// // Session settings override file settings
    /// store.Set("session.token", "temp-token");
    /// </code>
    /// </example>
    public SharinganBuilder UseInMemory(string? name = null, int priority = 50)
    {
        SettingsProviderOptions options = new()
        {
            Priority = priority,
            Scope = SettingsScope.Session,
            Serializer = _serializer
        };

        InMemorySettingsProvider provider = new(name, options, _serializer);
        _providers.Add(provider);

        return this;
    }

    /// <summary>
    /// Adds a custom provider to the settings store configuration.
    /// Use this method to add third-party or custom-implemented providers.
    /// </summary>
    /// <param name="provider">The provider instance to add. Cannot be null.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// This method allows integration of custom or third-party providers that implement
    /// <see cref="ISettingsProvider"/>. The provider's <see cref="ISettingsProvider.Priority"/>
    /// property is used to determine its position in the composite store.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var customProvider = new MyCustomProvider(options);
    /// var store = new SharinganBuilder()
    ///     .UseProvider(customProvider)
    ///     .Build();
    /// </code>
    /// </example>
    public SharinganBuilder UseProvider(ISettingsProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider, nameof(provider));
        _providers.Add(provider);
        return this;
    }

    /// <summary>
    /// Sets a specific provider as the target for all write operations.
    /// By default, writes go to the first writable provider in priority order.
    /// </summary>
    /// <param name="provider">The provider to use for write operations. Cannot be null.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// In composite store scenarios, you may want writes to go to a specific provider
    /// regardless of priority order. For example, you might want environment variables
    /// to have highest read priority but all writes to go to a JSON file.
    /// </para>
    /// <para>
    /// The specified provider must not be read-only, or write operations will fail
    /// at runtime with <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var jsonProvider = new JsonFileSettingsProvider();
    /// var store = new SharinganBuilder()
    ///     .UseEnvironmentVariables(priority: 100)  // Reads check env vars first
    ///     .UseProvider(jsonProvider)               // Also read from JSON
    ///     .WriteTargetProvider(jsonProvider)       // But all writes go to JSON
    ///     .Build();
    /// </code>
    /// </example>
    public SharinganBuilder WriteTargetProvider(ISettingsProvider provider)
    {
        _writeTarget = ThrowHelper.ThrowIfNull(provider, nameof(provider));
        return this;
    }

    /// <summary>
    /// Builds and returns the configured settings store.
    /// </summary>
    /// <returns>
    /// An <see cref="ISettingsStore"/> instance. If multiple providers are configured,
    /// returns a <see cref="CompositeSettingsProvider"/> that combines them.
    /// If only one provider is configured (and no write target is set), returns
    /// that provider directly. If no providers are configured, a default JSON file
    /// provider with user scope is created and returned.
    /// </returns>
    /// <remarks>
    /// <para>
    /// After calling <see cref="Build"/>, the builder can be reused to create additional
    /// stores with the same base configuration, or modified for different configurations.
    /// </para>
    /// <para>
    /// The returned store implements <see cref="IDisposable"/>. When disposed, all
    /// underlying providers are flushed and disposed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var store = new SharinganBuilder()
    ///     .WithApplicationName("MyApp")
    ///     .UseJsonFile()
    ///     .Build();
    /// 
    /// // Use the store
    /// store.Set("key", "value");
    /// 
    /// // Don't forget to dispose when done
    /// store.Dispose();
    /// </code>
    /// </example>
    public ISettingsStore Build()
    {
        if (_providers.Count == 0)
        {
            // Default: user-scoped JSON file
            UseJsonFile("settings.json", SettingsScope.User);
        }

        if (_providers.Count == 1 && _writeTarget is null)
        {
            return _providers[0];
        }

        return new CompositeSettingsProvider(_providers, _writeTarget);
    }

    /// <summary>
    /// Builds and returns the configured settings store as an <see cref="ISettingsProvider"/>.
    /// </summary>
    /// <returns>The settings provider that can be used in composite scenarios or for provider-specific operations.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the built store cannot be cast to <see cref="ISettingsProvider"/>.</exception>
    /// <remarks>
    /// This method is useful when you need access to provider-specific functionality
    /// like <see cref="ISettingsProvider.Reload"/> or the <see cref="ISettingsProvider.SettingsChanged"/> event.
    /// </remarks>
    public ISettingsProvider BuildProvider()
    {
        ISettingsStore store = Build();
        return store as ISettingsProvider ?? throw new InvalidOperationException("Could not build provider.");
    }

    /// <summary>
    /// Gets the current list of providers configured in this builder.
    /// This property is intended for internal use by extension methods and advanced scenarios.
    /// </summary>
    /// <value>A read-only list of all providers that have been added to this builder.</value>
    internal IReadOnlyList<ISettingsProvider> Providers => _providers;
}
