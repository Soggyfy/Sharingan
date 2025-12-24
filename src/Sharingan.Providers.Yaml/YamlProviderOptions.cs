using Sharingan.Abstractions;

namespace Sharingan.Providers.Yaml;

/// <summary>
/// Configuration options for <see cref="YamlFileSettingsProvider"/>, extending the base
/// <see cref="SettingsProviderOptions"/> with YAML file-specific settings.
/// </summary>
/// <remarks>
/// <para>
/// YAML (YAML Ain't Markup Language) is a human-readable data serialization format
/// commonly used for configuration files due to its clean syntax and readability.
/// This provider uses the YamlDotNet library for parsing and generation.
/// </para>
/// <para>
/// YAML features:
/// <list type="bullet">
/// <item><description>Clean, indentation-based syntax</description></item>
/// <item><description>Native support for lists, dictionaries, and complex types</description></item>
/// <item><description>Comments for documentation</description></item>
/// <item><description>Multiline string support</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new YamlProviderOptions
/// {
///     FilePath = "config.yaml",
///     UseAtomicWrites = true
/// };
/// </code>
/// </example>
/// <seealso cref="YamlFileSettingsProvider"/>
public class YamlProviderOptions : SettingsProviderOptions
{
    /// <summary>
    /// Gets or sets the file path for the YAML settings file.
    /// Can be relative (resolved based on scope) or absolute.
    /// </summary>
    /// <value>The path to the YAML file. Default is "settings.yaml".</value>
    public string FilePath { get; set; } = "settings.yaml";

    /// <summary>
    /// Gets or sets whether to create the YAML file if it doesn't exist.
    /// When enabled, a new file with a comment header is created.
    /// </summary>
    /// <value><c>true</c> to create the file automatically; <c>false</c> to leave it missing. Default is <c>true</c>.</value>
    public bool CreateIfNotExists { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use atomic writes when saving the file.
    /// Atomic writes prevent corruption by writing to a temp file first.
    /// </summary>
    /// <value><c>true</c> for atomic writes; <c>false</c> for direct writes. Default is <c>true</c>.</value>
    public bool UseAtomicWrites { get; set; } = true;
}
