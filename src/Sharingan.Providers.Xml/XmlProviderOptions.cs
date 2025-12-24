using Sharingan.Abstractions;

namespace Sharingan.Providers.Xml;

/// <summary>
/// Configuration options for <see cref="XmlFileSettingsProvider"/>, extending the base
/// <see cref="SettingsProviderOptions"/> with XML file-specific settings.
/// </summary>
/// <remarks>
/// <para>
/// XML provides a widely-supported, self-documenting format for configuration.
/// This provider uses System.Xml.Linq (LINQ to XML) for efficient parsing and generation.
/// </para>
/// <para>
/// Hierarchical keys (e.g., "database.connection.host") are stored as nested XML elements.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new XmlProviderOptions
/// {
///     FilePath = "settings.xml",
///     RootElementName = "Configuration",
///     UseAtomicWrites = true
/// };
/// </code>
/// </example>
/// <seealso cref="XmlFileSettingsProvider"/>
public class XmlProviderOptions : SettingsProviderOptions
{
    /// <summary>
    /// Gets or sets the file path for the XML settings file.
    /// Can be relative (resolved based on scope) or absolute.
    /// </summary>
    /// <value>The path to the XML file. Default is "settings.xml".</value>
    public string FilePath { get; set; } = "settings.xml";

    /// <summary>
    /// Gets or sets whether to create the XML file if it doesn't exist.
    /// When enabled, a new file with an empty root element is created.
    /// </summary>
    /// <value><c>true</c> to create the file automatically; <c>false</c> to leave it missing. Default is <c>true</c>.</value>
    public bool CreateIfNotExists { get; set; } = true;

    /// <summary>
    /// Gets or sets the root element name for the XML document.
    /// </summary>
    /// <value>The root element name. Default is "Settings".</value>
    /// <remarks>
    /// This element wraps all settings values. Example with "Settings" root:
    /// <code>
    /// &lt;Settings&gt;
    ///   &lt;Theme&gt;dark&lt;/Theme&gt;
    /// &lt;/Settings&gt;
    /// </code>
    /// </remarks>
    public string RootElementName { get; set; } = "Settings";

    /// <summary>
    /// Gets or sets whether to use atomic writes when saving the file.
    /// Atomic writes prevent corruption by writing to a temp file first.
    /// </summary>
    /// <value><c>true</c> for atomic writes; <c>false</c> for direct writes. Default is <c>true</c>.</value>
    public bool UseAtomicWrites { get; set; } = true;
}
