using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sharingan.Abstractions;
using System.Reflection;

namespace Sharingan.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring Sharingan with Microsoft.Extensions.DependencyInjection.
/// Enables seamless integration of Sharingan settings stores with .NET dependency injection containers.
/// </summary>
/// <remarks>
/// <para>
/// This class provides methods to register Sharingan settings stores with the .NET dependency
/// injection container. Once registered, stores can be injected into services using constructor
/// injection with <see cref="ISettingsStore"/>, <see cref="ISettingsProvider"/>, or
/// <see cref="IObservableSettingsStore"/> depending on the store implementation.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item><description>Singleton registration for application-wide settings access</description></item>
/// <item><description>Full builder configuration support</description></item>
/// <item><description>Quick setup with default JSON file configuration</description></item>
/// <item><description>Options pattern integration for strongly-typed settings</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Registering Sharingan with dependency injection:
/// <code>
/// services.AddSharingan(builder =>
/// {
///     builder.WithApplicationName("MyApp");
///     builder.UseJsonFile("settings.json");
///     builder.UseEnvironmentVariables(prefix: "MYAPP_");
/// });
/// 
/// // Later, inject ISettingsStore into your services:
/// public class MyService
/// {
///     private readonly ISettingsStore _settings;
///     
///     public MyService(ISettingsStore settings)
///     {
///         _settings = settings;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="SharinganBuilder"/>
/// <seealso cref="ISettingsStore"/>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a Sharingan settings store to the service collection as a singleton.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">A delegate to configure the Sharingan settings store using <see cref="SharinganBuilder"/>.</param>
    /// <returns>The <paramref name="services"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// The settings store is registered as a singleton, meaning the same instance is shared
    /// across all services that inject it. This is the recommended pattern for settings stores.
    /// </para>
    /// <para>
    /// The following service registrations are made (where applicable):
    /// <list type="bullet">
    /// <item><description><see cref="ISettingsStore"/>: The base store interface</description></item>
    /// <item><description><see cref="ISettingsProvider"/>: If the store implements this interface</description></item>
    /// <item><description><see cref="IObservableSettingsStore"/>: If the store implements this interface</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddSharingan(builder =>
    /// {
    ///     builder.WithApplicationName("MyApp")
    ///            .WithOrganizationName("MyCompany")
    ///            .UseJsonFile("settings.json", SettingsScope.User)
    ///            .UseEnvironmentVariables(prefix: "MYAPP_");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddSharingan(
        this IServiceCollection services,
        Action<SharinganBuilder> configure)
    {
        SharinganBuilder builder = new();
        configure(builder);

        ISettingsStore store = builder.Build();

        services.TryAddSingleton(store);
        services.TryAddSingleton<ISettingsStore>(sp => sp.GetRequiredService<ISettingsStore>());

        if (store is ISettingsProvider provider)
        {
            services.TryAddSingleton(provider);
        }

        if (store is IObservableSettingsStore observable)
        {
            services.TryAddSingleton(observable);
        }

        return services;
    }

    /// <summary>
    /// Adds Sharingan with a default JSON file configuration suitable for simple applications.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="applicationName">The application name for path resolution.</param>
    /// <param name="organizationName">Optional organization name for path resolution.</param>
    /// <returns>The <paramref name="services"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="applicationName"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// This overload provides a quick setup with sensible defaults:
    /// <list type="bullet">
    /// <item><description>File: "settings.json"</description></item>
    /// <item><description>Scope: User (stored in user's application data directory)</description></item>
    /// <item><description>Format: JSON with pretty-printing</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple setup with just application name
    /// services.AddSharingan("MyApp");
    /// 
    /// // With organization name
    /// services.AddSharingan("MyApp", "MyCompany");
    /// </code>
    /// </example>
    public static IServiceCollection AddSharingan(
        this IServiceCollection services,
        string applicationName,
        string? organizationName = null)
    {
        return services.AddSharingan(builder =>
        {
            builder.WithApplicationName(applicationName);

            if (!string.IsNullOrEmpty(organizationName))
            {
                builder.WithOrganizationName(organizationName!);
            }

            builder.UseJsonFile("settings.json", SettingsScope.User);
        });
    }

    /// <summary>
    /// Adds a strongly-typed settings section as an options pattern binding.
    /// Settings are loaded from the Sharingan store and configured as <see cref="Microsoft.Extensions.Options.IOptions{TSettings}"/>.
    /// </summary>
    /// <typeparam name="TSettings">The settings class type. Must be a class with a public parameterless constructor.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="key">Optional key for the settings section. If null, the type name is used.</param>
    /// <returns>The <paramref name="services"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// This method integrates Sharingan with the .NET Options pattern, allowing you to
    /// inject strongly-typed settings objects using <see cref="Microsoft.Extensions.Options.IOptions{TSettings}"/>.
    /// </para>
    /// <para>
    /// The settings are loaded from the Sharingan store using the specified key (or type name).
    /// If the key doesn't exist, a new instance of <typeparamref name="TSettings"/> is created
    /// with default values.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register the settings type
    /// services.AddSharingan("MyApp");
    /// services.AddSharinganOptions&lt;DatabaseSettings&gt;("Database");
    /// 
    /// // Inject and use in a service
    /// public class MyService
    /// {
    ///     private readonly DatabaseSettings _dbSettings;
    ///     
    ///     public MyService(IOptions&lt;DatabaseSettings&gt; options)
    ///     {
    ///         _dbSettings = options.Value;
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddSharinganOptions<TSettings>(
        this IServiceCollection services,
        string? key = null) where TSettings : class, new()
    {
        string settingsKey = key ?? typeof(TSettings).Name;

        services.AddOptions<TSettings>()
            .Configure<ISettingsStore>((settings, store) =>
            {
                TSettings loaded = store.Get(settingsKey, new TSettings());
                CopyProperties(loaded, settings);
            });

        return services;
    }

    /// <summary>
    /// Copies property values from a source object to a target object using reflection.
    /// Only public properties with both getter and setter are copied.
    /// </summary>
    /// <typeparam name="T">The type of objects to copy between.</typeparam>
    /// <param name="source">The source object to copy from.</param>
    /// <param name="target">The target object to copy to.</param>
    private static void CopyProperties<T>(T source, T target) where T : class
    {
        foreach (PropertyInfo property in typeof(T).GetProperties())
        {
            if (property.CanRead && property.CanWrite)
            {
                property.SetValue(target, property.GetValue(source));
            }
        }
    }
}
