using Sharingan.Abstractions;

namespace Sharingan;

/// <summary>
/// Static convenience class for quick access to settings using a default singleton instance.
/// Provides a pre-configured settings store that uses JSON file storage in the user scope,
/// suitable for simple applications that don't require custom configuration.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Settings"/> class provides a static entry point for applications that need
/// simple, out-of-the-box settings management without explicit configuration. It exposes a
/// <see cref="Default"/> singleton instance that is lazily initialized with sensible defaults.
/// </para>
/// <para>
/// By default, settings are stored in a file named "settings.json" located in the user's
/// application data directory (e.g., %APPDATA% on Windows, ~/.config on Linux).
/// </para>
/// <para>
/// For applications requiring custom configuration (multiple providers, encryption, specific
/// file paths, etc.), use the <see cref="CreateBuilder"/> method or <see cref="SharinganBuilder"/>
/// directly to create a customized settings store.
/// </para>
/// </remarks>
/// <example>
/// Using the default settings store:
/// <code>
/// // Store a setting
/// Settings.Default.Set("app.theme", "dark");
/// 
/// // Retrieve a setting with a default value
/// var volume = Settings.Default.Get("app.volume", 75);
/// 
/// // Persist changes
/// Settings.Default.Flush();
/// </code>
/// </example>
/// <example>
/// Using a custom settings store:
/// <code>
/// // Create a custom settings store
/// var customStore = Settings.CreateBuilder()
///     .WithApplicationName("MyApp")
///     .UseJsonFile("config.json", SettingsScope.User)
///     .UseEnvironmentVariables(prefix: "MYAPP_")
///     .Build();
/// 
/// // Optionally set it as the default
/// Settings.SetDefault(customStore);
/// </code>
/// </example>
/// <seealso cref="SharinganBuilder"/>
/// <seealso cref="ISettingsStore"/>
public static class Settings
{
    /// <summary>
    /// Lazy initializer for the default settings store, ensuring thread-safe initialization
    /// with full execution and publication guarantees.
    /// </summary>
    private static readonly Lazy<ISettingsStore> _default = new(CreateDefaultStore, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Gets the default settings store instance. This property returns the custom default store
    /// if one has been set via <see cref="SetDefault"/>, otherwise returns the built-in default
    /// store which uses JSON file storage in the user scope.
    /// </summary>
    /// <value>
    /// An <see cref="ISettingsStore"/> instance that can be used to read and write settings.
    /// If a custom default has been set, that instance is returned; otherwise, a lazily-created
    /// default instance using JSON file storage is returned.
    /// </value>
    /// <remarks>
    /// <para>
    /// The default store is lazily initialized on first access using a JSON file provider with
    /// the following configuration:
    /// <list type="bullet">
    /// <item><description>File name: "settings.json"</description></item>
    /// <item><description>Scope: User (stored in the user's application data directory)</description></item>
    /// <item><description>Format: JSON with pretty-printing and camelCase naming</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The default store is thread-safe and suitable for use from multiple threads concurrently.
    /// </para>
    /// </remarks>
    public static ISettingsStore? Default { get => field ?? _default.Value; private set; }

    /// <summary>
    /// Sets a custom settings store as the default instance. This replaces the built-in default
    /// store with a custom-configured store for the lifetime of the application.
    /// </summary>
    /// <param name="store">The <see cref="ISettingsStore"/> instance to use as the default.
    /// This store will be returned by subsequent calls to <see cref="Default"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is <c>null</c>.
    /// Use <see cref="ResetDefault"/> to revert to the built-in default store.</exception>
    /// <remarks>
    /// <para>
    /// This method allows you to configure a custom settings store (e.g., with multiple providers,
    /// encryption, or specific file paths) and make it the application-wide default.
    /// </para>
    /// <para>
    /// If the built-in default store has already been accessed and initialized before calling
    /// this method, that instance will no longer be returned, but it will not be disposed.
    /// The caller is responsible for disposing any custom stores when no longer needed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var myStore = Settings.CreateBuilder()
    ///     .WithApplicationName("MyApp")
    ///     .UseJsonFile("settings.json")
    ///     .Build();
    ///     
    /// Settings.SetDefault(myStore);
    /// </code>
    /// </example>
    public static void SetDefault(ISettingsStore store)
    {
        Default = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Resets the default settings store to the built-in default, discarding any custom default
    /// that was previously set via <see cref="SetDefault"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After calling this method, the <see cref="Default"/> property will return the built-in
    /// default store (JSON file in user scope). If the built-in default was previously initialized,
    /// the same instance will be returned.
    /// </para>
    /// <para>
    /// This method does not dispose the custom store that was previously set. The caller is
    /// responsible for disposing any custom stores when they are no longer needed.
    /// </para>
    /// </remarks>
    public static void ResetDefault()
    {
        Default = null;
    }

    /// <summary>
    /// Creates a new builder for configuring a custom settings store with multiple providers,
    /// custom serializers, encryption, and other advanced options.
    /// </summary>
    /// <returns>A new <see cref="SharinganBuilder"/> instance that can be used to configure
    /// and build a custom <see cref="ISettingsStore"/>.</returns>
    /// <remarks>
    /// <para>
    /// The builder pattern provides a fluent API for configuring complex settings store setups.
    /// Common configurations include:
    /// <list type="bullet">
    /// <item><description>Multiple providers with priority-based resolution</description></item>
    /// <item><description>Environment variable overlays</description></item>
    /// <item><description>Encrypted storage for sensitive data</description></item>
    /// <item><description>Custom file paths and formats (JSON, INI, YAML, XML, TOML)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var store = Settings.CreateBuilder()
    ///     .WithApplicationName("MyApp")
    ///     .WithOrganizationName("MyCompany")
    ///     .UseEnvironmentVariables(prefix: "MYAPP_", priority: 100)
    ///     .UseJsonFile("settings.json", SettingsScope.User, priority: 50)
    ///     .UseJsonFile("defaults.json", SettingsScope.Application, priority: 0)
    ///     .Build();
    /// </code>
    /// </example>
    /// <seealso cref="SharinganBuilder"/>
    public static SharinganBuilder CreateBuilder()
    {
        return new();
    }

    /// <summary>
    /// Creates the default settings store instance using a JSON file provider with user scope.
    /// This method is called lazily when the <see cref="Default"/> property is first accessed
    /// and no custom default has been set.
    /// </summary>
    /// <returns>A new <see cref="ISettingsStore"/> configured with JSON file storage in user scope.</returns>
    private static ISettingsStore CreateDefaultStore()
    {
        return new SharinganBuilder()
            .UseJsonFile("settings.json", SettingsScope.User)
            .Build();
    }
}
