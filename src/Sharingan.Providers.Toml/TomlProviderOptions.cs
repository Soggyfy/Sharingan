using Sharingan.Abstractions;

namespace Sharingan.Providers.Toml;

/// <summary>
/// Configuration options for <see cref="TomlFileSettingsProvider"/>, extending the base
/// <see cref="SettingsProviderOptions"/> with TOML file-specific settings.
/// </summary>
/// <remarks>
/// <para>
/// TOML (Tom's Obvious Minimal Language) is a configuration file format that is easy to read
/// and write, with clear semantics. This provider uses the Tomlyn library for parsing and generation.
/// </para>
/// <para>
/// TOML supports:
/// <list type="bullet">
/// <item><description>Hierarchical tables (sections) using [table] syntax</description></item>
/// <item><description>Native support for dates, arrays, and nested objects</description></item>
/// <item><description>Comments for documentation</description></item>
/// <item><description>Multiline strings</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new TomlProviderOptions
/// {
///     FilePath = "config.toml",
///     UseAtomicWrites = true
/// };
/// </code>
/// </example>
/// <seealso cref="TomlFileSettingsProvider"/>
public class TomlProviderOptions : SettingsProviderOptions
{
    /// <summary>
    /// Gets or sets the file path for the TOML settings file.
    /// Can be relative (resolved based on scope) or absolute.
    /// </summary>
    /// <value>The path to the TOML file. Default is "settings.toml".</value>
    public string FilePath { get; set; } = "settings.toml";

    /// <summary>
    /// Gets or sets whether to create the TOML file if it doesn't exist.
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
