using Sharingan.Abstractions;

namespace Sharingan.Providers;

/// <summary>
/// Configuration options for <see cref="JsonFileSettingsProvider"/>, extending the base
/// <see cref="SettingsProviderOptions"/> with JSON file-specific settings.
/// </summary>
/// <remarks>
/// <para>
/// This options class provides configuration for the JSON file settings provider, including:
/// <list type="bullet">
/// <item><description>File path configuration (relative or absolute)</description></item>
/// <item><description>Automatic file creation</description></item>
/// <item><description>External file change monitoring</description></item>
/// <item><description>Retry logic for handling file lock contention</description></item>
/// <item><description>Atomic write operations for data integrity</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Configuring JSON file provider options:
/// <code>
/// var options = new JsonFileProviderOptions
/// {
///     FilePath = "config.json",
///     Scope = SettingsScope.User,
///     CreateIfNotExists = true,
///     WatchForChanges = true,
///     UseAtomicWrites = true
/// };
/// 
/// var provider = new JsonFileSettingsProvider(options);
/// </code>
/// </example>
/// <seealso cref="JsonFileSettingsProvider"/>
/// <seealso cref="SettingsProviderOptions"/>
public class JsonFileProviderOptions : SettingsProviderOptions
{
    /// <summary>
    /// Gets or sets the file path for the JSON settings file.
    /// Can be a relative path (resolved based on scope and application name) or an absolute path.
    /// </summary>
    /// <value>The path to the JSON settings file. Default is "settings.json".</value>
    /// <remarks>
    /// <para>
    /// If a relative path is specified, it is combined with the base path determined by the
    /// <see cref="SettingsProviderOptions.Scope"/>, <see cref="SettingsProviderOptions.ApplicationName"/>,
    /// and <see cref="SettingsProviderOptions.OrganizationName"/> properties.
    /// </para>
    /// <para>
    /// If an absolute path is specified, it is used directly without modification.
    /// </para>
    /// </remarks>
    public string FilePath { get; set; } = "settings.json";

    /// <summary>
    /// Gets or sets whether to create the JSON file if it doesn't exist.
    /// When enabled, a new empty JSON object file is created on first access.
    /// </summary>
    /// <value><c>true</c> to create the file automatically; <c>false</c> to leave it missing. Default is <c>true</c>.</value>
    /// <remarks>
    /// When enabled, the provider will create the file with an empty JSON object ("{}") content
    /// if it doesn't exist. The parent directory is also created if necessary.
    /// </remarks>
    public bool CreateIfNotExists { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to watch for external file changes and automatically reload settings.
    /// When enabled, a <see cref="FileSystemWatcher"/> monitors the file for modifications.
    /// </summary>
    /// <value><c>true</c> to enable file watching; <c>false</c> to disable it. Default is <c>false</c>.</value>
    /// <remarks>
    /// <para>
    /// When enabled, the provider uses a <see cref="FileSystemWatcher"/> to detect changes made
    /// to the settings file by external processes or manual editing. When changes are detected,
    /// the provider automatically reloads the settings and raises the
    /// <see cref="ISettingsProvider.SettingsChanged"/> event.
    /// </para>
    /// <para>
    /// File watching adds some overhead and may not work correctly on all file systems
    /// (particularly network shares). Enable only when external modification detection is required.
    /// </para>
    /// </remarks>
    public bool WatchForChanges { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for file operations when the file is locked.
    /// </summary>
    /// <value>The maximum number of retry attempts. Default is 3.</value>
    /// <remarks>
    /// File operations may fail with <see cref="IOException"/> when the file is temporarily locked
    /// by another process. The provider will retry the operation up to this many times before
    /// throwing an exception.
    /// </remarks>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay in milliseconds between retry attempts for file operations.
    /// </summary>
    /// <value>The delay between retries in milliseconds. Default is 100ms.</value>
    /// <remarks>
    /// This delay is applied between retry attempts when a file operation fails due to a lock.
    /// A small delay allows other processes to complete their file operations.
    /// </remarks>
    public int RetryDelayMilliseconds { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to use atomic writes when saving the settings file.
    /// Atomic writes prevent data corruption by writing to a temporary file first, then renaming.
    /// </summary>
    /// <value><c>true</c> to use atomic writes; <c>false</c> for direct writes. Default is <c>true</c>.</value>
    /// <remarks>
    /// <para>
    /// When enabled, the provider writes settings to a temporary file (filename + ".tmp") first,
    /// then renames it to the target filename. This ensures the settings file is never in a
    /// partially-written state, preventing corruption if the process is interrupted during a write.
    /// </para>
    /// <para>
    /// This is recommended for all production use. Disable only if the file system doesn't support
    /// atomic rename operations (rare).
    /// </para>
    /// </remarks>
    public bool UseAtomicWrites { get; set; } = true;
}
