using Microsoft.Win32;
using Sharingan.Abstractions;

namespace Sharingan.Providers.Registry;

/// <summary>
/// Configuration options for <see cref="RegistrySettingsProvider"/>, extending the base
/// <see cref="SettingsProviderOptions"/> with Windows Registry-specific settings.
/// </summary>
/// <remarks>
/// <para>
/// The Registry provider stores settings in the Windows Registry, providing:
/// <list type="bullet">
/// <item><description>System-native storage for Windows applications</description></item>
/// <item><description>Automatic backup via Windows system restore</description></item>
/// <item><description>ACL-based security for sensitive settings</description></item>
/// <item><description>Immediate persistence (no flush required)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Note:</strong> This provider is Windows-only and will throw
/// <see cref="PlatformNotSupportedException"/> on other operating systems.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new RegistryProviderOptions
/// {
///     Scope = SettingsScope.User,
///     Hive = RegistryHive.CurrentUser,
///     SubKeyPath = @"Software\MyCompany\MyApp",
///     CreateIfNotExists = true
/// };
/// </code>
/// </example>
/// <seealso cref="RegistrySettingsProvider"/>
public class RegistryProviderOptions : SettingsProviderOptions
{
    /// <summary>
    /// Gets or sets the registry hive to use for storing settings.
    /// When null, the hive is automatically selected based on scope.
    /// </summary>
    /// <value>
    /// The registry hive, or null for automatic selection.
    /// Default is null (auto-select: <see cref="SettingsScope.User"/> → <see cref="RegistryHive.CurrentUser"/>,
    /// <see cref="SettingsScope.Machine"/> → <see cref="RegistryHive.LocalMachine"/>).
    /// </value>
    public RegistryHive? Hive { get; set; }

    /// <summary>
    /// Gets or sets the sub-key path under the selected hive.
    /// This is where settings values are stored.
    /// </summary>
    /// <value>
    /// The registry path under the hive. If null, defaults to "Software\{OrganizationName}\{ApplicationName}".
    /// </value>
    /// <remarks>
    /// Do not include a leading backslash. Example: "Software\MyCompany\MyApp"
    /// </remarks>
    public string? SubKeyPath { get; set; }

    /// <summary>
    /// Gets or sets whether to create the registry key if it doesn't exist.
    /// </summary>
    /// <value><c>true</c> to create the key automatically; <c>false</c> to throw if missing. Default is <c>true</c>.</value>
    public bool CreateIfNotExists { get; set; } = true;
}
