using Sharingan.Abstractions;

namespace Sharingan.Providers.Ini;

/// <summary>
/// Extension methods for <see cref="SharinganBuilder"/> to add INI file provider support.
/// </summary>
/// <remarks>
/// INI files provide a simple, human-readable format for configuration with section-based organization.
/// </remarks>
/// <seealso cref="IniFileSettingsProvider"/>
/// <seealso cref="IniProviderOptions"/>
public static class SharinganBuilderExtensions
{
    /// <summary>
    /// Adds an INI file provider to the settings store configuration.
    /// INI files use a section-based format with [Section] headers and key=value pairs.
    /// </summary>
    /// <param name="builder">The Sharingan builder instance.</param>
    /// <param name="filePath">The path to the INI file. Can be relative or absolute. Default is "settings.ini".</param>
    /// <param name="scope">The settings scope determining where the file is stored. Default is <see cref="SettingsScope.User"/>.</param>
    /// <param name="priority">The provider priority for composite stores. Default is 0.</param>
    /// <param name="configure">Optional action to configure additional INI provider options.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var store = new SharinganBuilder()
    ///     .UseIniFile("config.ini", configure: options =>
    ///     {
    ///         options.DefaultSection = "Application";
    ///     })
    ///     .Build();
    /// 
    /// // Stored as [Application] -> theme=dark
    /// store.Set("theme", "dark");
    /// 
    /// // Stored as [Database] -> host=localhost
    /// store.Set("Database.host", "localhost");
    /// </code>
    /// </example>
    public static SharinganBuilder UseIniFile(
        this SharinganBuilder builder,
        string filePath = "settings.ini",
        SettingsScope scope = SettingsScope.User,
        int priority = 0,
        Action<IniProviderOptions>? configure = null)
    {
        IniProviderOptions options = new()
        {
            FilePath = filePath,
            Scope = scope,
            Priority = priority
        };
        configure?.Invoke(options);
        return builder.UseProvider(new IniFileSettingsProvider(options));
    }
}
