using Microsoft.Extensions.Configuration;
using Sharingan.Abstractions;

namespace Sharingan.Extensions.Configuration;

/// <summary>
/// Extension methods for integrating Sharingan settings stores with Microsoft.Extensions.Configuration.
/// Enables using Sharingan as a configuration source in .NET applications that use the IConfiguration pattern.
/// </summary>
/// <remarks>
/// <para>
/// This class provides seamless integration between Sharingan settings stores and the
/// Microsoft.Extensions.Configuration infrastructure. Once registered, Sharingan settings
/// can be accessed through the standard <see cref="IConfiguration"/> interface.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item><description>Full integration with the .NET configuration system</description></item>
/// <item><description>Automatic key format conversion (dots to colons for IConfiguration compatibility)</description></item>
/// <item><description>Support for configuration change notifications when using observable stores</description></item>
/// <item><description>Works with dependency injection and options pattern</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Adding Sharingan as a configuration source:
/// <code>
/// var config = new ConfigurationBuilder()
///     .AddSharingan(builder =>
///     {
///         builder.WithApplicationName("MyApp");
///         builder.UseJsonFile("settings.json");
///         builder.UseEnvironmentVariables(prefix: "MYAPP_");
///     })
///     .Build();
/// 
/// // Access settings through IConfiguration
/// var theme = config["ui:theme"];
/// var connectionString = config.GetConnectionString("Default");
/// </code>
/// </example>
/// <seealso cref="SharinganConfigurationSource"/>
/// <seealso cref="SharinganConfigurationProvider"/>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds a Sharingan settings store as a configuration source to the configuration builder.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add the source to.</param>
    /// <param name="configure">A delegate to configure the Sharingan settings store using <see cref="SharinganBuilder"/>.</param>
    /// <returns>The <paramref name="builder"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// The configured Sharingan store is wrapped in a <see cref="SharinganConfigurationSource"/>
    /// and added to the configuration pipeline. Settings are loaded when <c>Build()</c> is called
    /// on the configuration builder.
    /// </para>
    /// <para>
    /// Sharingan key names using dots (e.g., "database.host") are converted to colon-separated
    /// format (e.g., "database:host") for IConfiguration compatibility.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// IConfiguration config = new ConfigurationBuilder()
    ///     .AddSharingan(builder => builder.UseJsonFile("appsettings.json"))
    ///     .AddEnvironmentVariables()
    ///     .Build();
    /// </code>
    /// </example>
    public static IConfigurationBuilder AddSharingan(
        this IConfigurationBuilder builder,
        Action<SharinganBuilder> configure)
    {
        SharinganBuilder sharinganBuilder = new();
        configure(sharinganBuilder);

        ISettingsStore store = sharinganBuilder.Build();
        return builder.Add(new SharinganConfigurationSource(store));
    }
}

/// <summary>
/// Configuration source that wraps a Sharingan settings store for use with
/// Microsoft.Extensions.Configuration.
/// </summary>
/// <remarks>
/// This class implements <see cref="IConfigurationSource"/> to integrate Sharingan stores
/// into the .NET configuration system. It holds a reference to the settings store and
/// creates <see cref="SharinganConfigurationProvider"/> instances when built.
/// </remarks>
/// <seealso cref="SharinganConfigurationProvider"/>
/// <seealso cref="ConfigurationBuilderExtensions"/>
public class SharinganConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// The Sharingan settings store that provides configuration values.
    /// </summary>
    private readonly ISettingsStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharinganConfigurationSource"/> class
    /// with the specified settings store.
    /// </summary>
    /// <param name="store">The Sharingan settings store to use as the configuration source.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
    public SharinganConfigurationSource(ISettingsStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Builds a <see cref="SharinganConfigurationProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The configuration builder (not used but required by the interface).</param>
    /// <returns>A new <see cref="SharinganConfigurationProvider"/> instance.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SharinganConfigurationProvider(_store);
    }
}

/// <summary>
/// Configuration provider that reads settings from a Sharingan settings store
/// and exposes them through the Microsoft.Extensions.Configuration API.
/// </summary>
/// <remarks>
/// <para>
/// This provider loads all keys from the Sharingan store and makes them available
/// through the standard <see cref="IConfigurationProvider"/> interface. Key names
/// are converted from dot notation to colon notation for IConfiguration compatibility.
/// </para>
/// <para>
/// If the underlying store implements <see cref="IObservableSettingsStore"/>, the provider
/// automatically subscribes to change notifications and reloads when settings change.
/// </para>
/// </remarks>
/// <seealso cref="SharinganConfigurationSource"/>
public class SharinganConfigurationProvider : ConfigurationProvider, IDisposable
{
    /// <summary>
    /// The Sharingan settings store that provides configuration values.
    /// </summary>
    private readonly ISettingsStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharinganConfigurationProvider"/> class
    /// with the specified settings store.
    /// </summary>
    /// <param name="store">The Sharingan settings store to read settings from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
    /// <remarks>
    /// If the store implements <see cref="IObservableSettingsStore"/>, the provider
    /// subscribes to change notifications and automatically reloads when settings change.
    /// </remarks>
    public SharinganConfigurationProvider(ISettingsStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));

        if (_store is IObservableSettingsStore observable)
        {
            observable.SettingsChanged += OnSettingsChanged;
        }
    }

    /// <summary>
    /// Loads (or reloads) all settings from the Sharingan store into the configuration data dictionary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method clears the current data and reloads all keys from the store.
    /// Keys using dot notation (e.g., "database.host") are converted to colon notation
    /// (e.g., "database:host") for compatibility with <see cref="IConfiguration"/>.
    /// </para>
    /// <para>
    /// Only string values are loaded. Complex objects should be serialized as strings
    /// in the store or accessed directly through the Sharingan store interface.
    /// </para>
    /// </remarks>
    public override void Load()
    {
        Data.Clear();

        foreach (string key in _store.GetAllKeys())
        {
            string? value = _store.GetOrDefault<string>(key);
            if (value is not null)
            {
                // Convert dot notation to colon for IConfiguration
                string configKey = key.Replace('.', ':');
                Data[configKey] = value;
            }
        }
    }

    /// <summary>
    /// Handles settings change events from observable stores by reloading configuration.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments containing change details.</param>
    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        Load();
        OnReload();
    }

    /// <summary>
    /// Releases resources used by the provider, including unsubscribing from change events
    /// and disposing the underlying store if applicable.
    /// </summary>
    public void Dispose()
    {
        if (_store is IObservableSettingsStore observable)
        {
            observable.SettingsChanged -= OnSettingsChanged;
        }

        if (_store is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
