using Sharingan.Abstractions;

namespace Sharingan.Providers.Toml;

/// <summary>
/// Extension methods for <see cref="SharinganBuilder"/> to add TOML file provider support.
/// </summary>
/// <remarks>
/// TOML provides an expressive, human-readable configuration format with support for
/// tables, arrays, dates, and comments. It's particularly suitable for complex configurations.
/// </remarks>
/// <seealso cref="TomlFileSettingsProvider"/>
/// <seealso cref="TomlProviderOptions"/>
public static class SharinganBuilderExtensions
{
    /// <summary>
    /// Adds a TOML file provider to the settings store configuration.
    /// TOML files provide human-readable, semantically clear configuration.
    /// </summary>
    /// <param name="builder">The Sharingan builder instance.</param>
    /// <param name="filePath">The path to the TOML file. Can be relative or absolute. Default is "settings.toml".</param>
    /// <param name="scope">The settings scope determining where the file is stored. Default is <see cref="SettingsScope.User"/>.</param>
    /// <param name="priority">The provider priority for composite stores. Default is 0.</param>
    /// <param name="configure">Optional action to configure additional TOML provider options.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var store = new SharinganBuilder()
    ///     .UseTomlFile("config.toml", SettingsScope.User)
    ///     .Build();
    /// 
    /// // Hierarchical keys are stored as nested TOML tables
    /// store.Set("database.connection.host", "localhost");
    /// // Produces: [database.connection] -> host = "localhost"
    /// </code>
    /// </example>
    public static SharinganBuilder UseTomlFile(
        this SharinganBuilder builder,
        string filePath = "settings.toml",
        SettingsScope scope = SettingsScope.User,
        int priority = 0,
        Action<TomlProviderOptions>? configure = null)
    {
        TomlProviderOptions options = new()
        {
            FilePath = filePath,
            Scope = scope,
            Priority = priority
        };
        configure?.Invoke(options);
        return builder.UseProvider(new TomlFileSettingsProvider(options));
    }
}
