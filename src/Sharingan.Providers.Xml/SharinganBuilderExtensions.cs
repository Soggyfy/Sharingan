using Sharingan.Abstractions;

namespace Sharingan.Providers.Xml;

/// <summary>
/// Extension methods for <see cref="SharinganBuilder"/> to add XML file provider support.
/// </summary>
/// <remarks>
/// XML provides a standard, widely-compatible format for configuration with support for
/// hierarchical data, attributes, and namespaces.
/// </remarks>
/// <seealso cref="XmlFileSettingsProvider"/>
/// <seealso cref="XmlProviderOptions"/>
public static class SharinganBuilderExtensions
{
    /// <summary>
    /// Adds an XML file provider to the settings store configuration.
    /// XML files provide structured, hierarchical configuration storage.
    /// </summary>
    /// <param name="builder">The Sharingan builder instance.</param>
    /// <param name="filePath">The path to the XML file. Can be relative or absolute. Default is "settings.xml".</param>
    /// <param name="scope">The settings scope determining where the file is stored. Default is <see cref="SettingsScope.User"/>.</param>
    /// <param name="priority">The provider priority for composite stores. Default is 0.</param>
    /// <param name="configure">Optional action to configure additional XML provider options such as root element name.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var store = new SharinganBuilder()
    ///     .UseXmlFile("config.xml", configure: options =>
    ///     {
    ///         options.RootElementName = "AppConfiguration";
    ///     })
    ///     .Build();
    /// 
    /// store.Set("database.host", "localhost");
    /// // Produces:
    /// // &lt;AppConfiguration&gt;
    /// //   &lt;database&gt;
    /// //     &lt;host&gt;localhost&lt;/host&gt;
    /// //   &lt;/database&gt;
    /// // &lt;/AppConfiguration&gt;
    /// </code>
    /// </example>
    public static SharinganBuilder UseXmlFile(
        this SharinganBuilder builder,
        string filePath = "settings.xml",
        SettingsScope scope = SettingsScope.User,
        int priority = 0,
        Action<XmlProviderOptions>? configure = null)
    {
        XmlProviderOptions options = new()
        {
            FilePath = filePath,
            Scope = scope,
            Priority = priority
        };
        configure?.Invoke(options);
        return builder.UseProvider(new XmlFileSettingsProvider(options));
    }
}
