namespace Sharingan.Abstractions;

/// <summary>
/// Defines the scope for settings storage location, determining where settings are physically
/// stored and who can access them on the local system.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SettingsScope"/> enumeration is used to specify the logical and physical
/// location of settings storage. Each scope maps to platform-specific directories that provide
/// appropriate isolation and accessibility for different use cases.
/// </para>
/// <para>
/// Choosing the correct scope is important for:
/// <list type="bullet">
/// <item><description>Security: Machine-scope settings may require elevated permissions to modify</description></item>
/// <item><description>User isolation: User-scope settings are private to each user</description></item>
/// <item><description>Portability: Application-scope settings travel with the application</description></item>
/// <item><description>Persistence: Session-scope settings are lost when the application exits</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Using different scopes:
/// <code>
/// // User-specific settings (themes, preferences)
/// builder.UseJsonFile("preferences.json", SettingsScope.User);
/// 
/// // Machine-wide settings (shared configuration)
/// builder.UseJsonFile("config.json", SettingsScope.Machine);
/// 
/// // Portable settings (next to the executable)
/// builder.UseJsonFile("portable.json", SettingsScope.Application);
/// 
/// // Temporary session settings (in-memory or temp folder)
/// builder.UseInMemory(scope: SettingsScope.Session);
/// </code>
/// </example>
/// <seealso cref="SettingsProviderOptions"/>
/// <seealso cref="ISettingsProvider"/>
public enum SettingsScope
{
    /// <summary>
    /// User-specific settings stored in the current user's profile directory.
    /// These settings are private to the current user and not shared with other users on the same machine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Storage locations by platform:
    /// </para>
    /// <list type="bullet">
    /// <item><term>Windows</term><description><c>%APPDATA%\{OrganizationName}\{ApplicationName}\</c> (typically <c>C:\Users\{User}\AppData\Roaming\...</c>)</description></item>
    /// <item><term>Linux</term><description><c>~/.config/{OrganizationName}/{ApplicationName}/</c></description></item>
    /// <item><term>macOS</term><description><c>~/Library/Application Support/{OrganizationName}/{ApplicationName}/</c></description></item>
    /// <item><term>Android</term><description><c>{InternalStorage}/{OrganizationName}/{ApplicationName}/</c> (app's private internal storage)</description></item>
    /// <item><term>iOS</term><description><c>{AppSandbox}/Library/{OrganizationName}/{ApplicationName}/</c></description></item>
    /// </list>
    /// <para>
    /// This is the most commonly used scope and is appropriate for:
    /// <list type="bullet">
    /// <item><description>User interface preferences (theme, language, layout)</description></item>
    /// <item><description>Recently used files and history</description></item>
    /// <item><description>User-specific feature settings</description></item>
    /// <item><description>Authentication tokens and credentials (with encryption)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    User = 0,

    /// <summary>
    /// Machine-wide settings accessible to all users on the local machine.
    /// These settings are shared across all users and typically require elevated permissions to modify.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Storage locations by platform:
    /// </para>
    /// <list type="bullet">
    /// <item><term>Windows</term><description><c>%PROGRAMDATA%\{OrganizationName}\{ApplicationName}\</c> (typically <c>C:\ProgramData\...</c>)</description></item>
    /// <item><term>Linux</term><description><c>/etc/{OrganizationName}/{ApplicationName}/</c></description></item>
    /// <item><term>macOS</term><description><c>/Library/Application Support/{OrganizationName}/{ApplicationName}/</c></description></item>
    /// <item><term>Android</term><description>Falls back to User scope (app sandbox prevents shared storage)</description></item>
    /// <item><term>iOS</term><description>Falls back to User scope (app sandbox prevents shared storage)</description></item>
    /// </list>
    /// <para>
    /// This scope is appropriate for:
    /// <list type="bullet">
    /// <item><description>System-wide application configuration</description></item>
    /// <item><description>Shared default settings</description></item>
    /// <item><description>License information</description></item>
    /// <item><description>Administrator-managed configuration</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong> Writing to machine-scope locations may require administrator/root privileges.
    /// Applications should handle permission errors gracefully. On mobile platforms (Android/iOS),
    /// machine-wide settings are not supported due to app sandboxing, so this scope falls back to
    /// user-scoped storage.
    /// </para>
    /// </remarks>
    Machine = 1,

    /// <summary>
    /// Application-local settings stored in the same directory as the executable.
    /// These settings travel with the application and are suitable for portable installations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The storage location is the directory containing the application's entry assembly
    /// or <see cref="AppContext.BaseDirectory"/> as a fallback.
    /// </para>
    /// <para>
    /// This scope is appropriate for:
    /// <list type="bullet">
    /// <item><description>Portable applications that run from USB drives or network shares</description></item>
    /// <item><description>Application defaults that ship with the software</description></item>
    /// <item><description>Per-installation configuration (when multiple copies exist)</description></item>
    /// <item><description>Development and testing scenarios</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong> Writing to the application directory may fail if the application
    /// is installed in a protected location (such as Program Files on Windows) or if the
    /// current user lacks write permissions.
    /// </para>
    /// </remarks>
    Application = 2,

    /// <summary>
    /// Session-only settings that exist only for the duration of the application's execution.
    /// These settings are not persisted to disk and are lost when the application terminates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Session-scoped settings are typically stored in memory or in a temporary directory
    /// that is cleaned up when the application exits. The exact location depends on the
    /// provider implementation.
    /// </para>
    /// <para>
    /// This scope is appropriate for:
    /// <list type="bullet">
    /// <item><description>Temporary overrides and runtime configuration</description></item>
    /// <item><description>Test fixtures and mock settings</description></item>
    /// <item><description>Cached values that should not persist between sessions</description></item>
    /// <item><description>Settings that are computed at startup from other sources</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Typically used with <see cref="ISettingsProvider"/> implementations that store data in memory,
    /// such as the in-memory provider.
    /// </para>
    /// </remarks>
    Session = 3
}
