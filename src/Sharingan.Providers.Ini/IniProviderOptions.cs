using Sharingan.Abstractions;

namespace Sharingan.Providers.Ini;

/// <summary>
/// Configuration options for <see cref="IniFileSettingsProvider"/>, extending the base
/// <see cref="SettingsProviderOptions"/> with INI file-specific settings.
/// </summary>
/// <remarks>
/// <para>
/// INI files are a simple, human-readable configuration format with sections and key-value pairs.
/// This options class configures file location, section handling, and retry behavior.
/// </para>
/// <para>
/// Key format in INI provider:
/// <list type="bullet">
/// <item><description>"Section.Key" - Uses the specified section</description></item>
/// <item><description>"Key" - Uses the default section (configurable)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new IniProviderOptions
/// {
///     FilePath = "config.ini",
///     DefaultSection = "General",
///     CreateIfNotExists = true
/// };
/// </code>
/// </example>
/// <seealso cref="IniFileSettingsProvider"/>
public class IniProviderOptions : SettingsProviderOptions
{
    /// <summary>
    /// Gets or sets the file path for the INI settings file.
    /// Can be relative (resolved based on scope) or absolute.
    /// </summary>
    /// <value>The path to the INI file. Default is "settings.ini".</value>
    public string FilePath { get; set; } = "settings.ini";

    /// <summary>
    /// Gets or sets whether to create the INI file if it doesn't exist.
    /// When enabled, a new file with an empty default section is created.
    /// </summary>
    /// <value><c>true</c> to create the file automatically; <c>false</c> to leave it missing. Default is <c>true</c>.</value>
    public bool CreateIfNotExists { get; set; } = true;

    /// <summary>
    /// Gets or sets the default section name for settings without a section prefix.
    /// Keys without a dot separator are stored under this section.
    /// </summary>
    /// <value>The default section name. Default is "Settings".</value>
    /// <remarks>
    /// For example, with default section "Settings", the key "theme" is stored as:
    /// <code>
    /// [Settings]
    /// theme=dark
    /// </code>
    /// </remarks>
    public string DefaultSection { get; set; } = "Settings";

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for file operations when locked.
    /// </summary>
    /// <value>The maximum retry count. Default is 3.</value>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between retry attempts in milliseconds.
    /// </summary>
    /// <value>The delay in milliseconds. Default is 100.</value>
    public int RetryDelayMilliseconds { get; set; } = 100;
}
