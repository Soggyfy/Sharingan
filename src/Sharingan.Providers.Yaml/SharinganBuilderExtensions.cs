using Sharingan.Abstractions;

namespace Sharingan.Providers.Yaml;

/// <summary>
/// Extension methods for <see cref="SharinganBuilder"/> to add YAML file provider support.
/// </summary>
/// <remarks>
/// YAML provides a clean, human-readable configuration format popular in DevOps,
/// containerization, and modern application configuration scenarios.
/// </remarks>
/// <seealso cref="YamlFileSettingsProvider"/>
/// <seealso cref="YamlProviderOptions"/>
public static class SharinganBuilderExtensions
{
    /// <summary>
    /// Adds a YAML file provider to the settings store configuration.
    /// YAML files provide clean, indentation-based configuration storage.
    /// </summary>
    /// <param name="builder">The Sharingan builder instance.</param>
    /// <param name="filePath">The path to the YAML file. Can be relative or absolute. Default is "settings.yaml".</param>
    /// <param name="scope">The settings scope determining where the file is stored. Default is <see cref="SettingsScope.User"/>.</param>
    /// <param name="priority">The provider priority for composite stores. Default is 0.</param>
    /// <param name="configure">Optional action to configure additional YAML provider options.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var store = new SharinganBuilder()
    ///     .UseYamlFile("config.yaml", SettingsScope.User)
    ///     .Build();
    /// 
    /// store.Set("theme", "dark");
    /// store.Set("volume", 75);
    /// // Produces:
    /// // theme: dark
    /// // volume: 75
    /// </code>
    /// </example>
    public static SharinganBuilder UseYamlFile(
        this SharinganBuilder builder,
        string filePath = "settings.yaml",
        SettingsScope scope = SettingsScope.User,
        int priority = 0,
        Action<YamlProviderOptions>? configure = null)
    {
        YamlProviderOptions options = new()
        {
            FilePath = filePath,
            Scope = scope,
            Priority = priority
        };
        configure?.Invoke(options);
        return builder.UseProvider(new YamlFileSettingsProvider(options));
    }
}
