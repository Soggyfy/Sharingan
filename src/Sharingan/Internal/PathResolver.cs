using Sharingan.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Sharingan.Internal;

/// <summary>
/// Resolves file paths based on settings scope and platform, providing cross-platform
/// support for determining appropriate storage locations for application settings.
/// </summary>
/// <remarks>
/// <para>
/// This resolver handles platform-specific path conventions for Windows, Linux, macOS,
/// Android, and iOS, ensuring settings are stored in standard locations appropriate
/// for each operating system.
/// </para>
/// <para>
/// The resolver supports:
/// <list type="bullet">
/// <item><description>User-scoped paths (per-user application data)</description></item>
/// <item><description>Machine-scoped paths (shared system-wide data)</description></item>
/// <item><description>Application-scoped paths (next to the executable)</description></item>
/// <item><description>Session-scoped paths (temporary directory)</description></item>
/// </list>
/// </para>
/// <para>
/// Path sanitization is performed to ensure directory names are valid for the file system,
/// replacing invalid characters with underscores.
/// </para>
/// </remarks>
public static class PathResolver
{
    /// <summary>
    /// Resolves the base directory for a given settings scope, taking into account
    /// the application name, organization name, and current platform conventions.
    /// </summary>
    /// <param name="scope">The settings scope that determines the base location.</param>
    /// <param name="applicationName">Optional application name for path construction. If null, the entry assembly name is used.</param>
    /// <param name="organizationName">Optional organization name to add as a parent directory.</param>
    /// <returns>The full path to the base directory for the specified scope.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown <paramref name="scope"/> value is provided.</exception>
    /// <remarks>
    /// <para>
    /// Platform-specific locations for <see cref="SettingsScope.User"/>:
    /// <list type="bullet">
    /// <item><description>Windows: <c>%APPDATA%\{Organization}\{Application}</c></description></item>
    /// <item><description>Linux: <c>~/.config/{Organization}/{Application}</c></description></item>
    /// <item><description>macOS: <c>~/Library/Application Support/{Organization}/{Application}</c></description></item>
    /// <item><description>Android: <c>{AppContext.BaseDirectory}/{Organization}/{Application}</c> (internal storage)</description></item>
    /// <item><description>iOS: <c>{AppContext.BaseDirectory}/Library/{Organization}/{Application}</c></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static string GetBasePath(SettingsScope scope, string? applicationName = null, string? organizationName = null)
    {
        string appName = applicationName ?? GetDefaultApplicationName();
        string? orgName = organizationName;

        return scope switch
        {
            SettingsScope.User => GetUserPath(appName, orgName),
            SettingsScope.Machine => GetMachinePath(appName, orgName),
            SettingsScope.Application => GetApplicationPath(),
            SettingsScope.Session => Path.GetTempPath(),
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown settings scope.")
        };
    }

    /// <summary>
    /// Resolves the full file path for a settings file, combining the scope-based directory
    /// with the specified file name. If the file name is already an absolute path, it is returned unchanged.
    /// </summary>
    /// <param name="fileName">The settings file name (e.g., "settings.json"). Can be relative or absolute.</param>
    /// <param name="scope">The settings scope that determines the base directory for relative paths.</param>
    /// <param name="applicationName">Optional application name for path construction.</param>
    /// <param name="organizationName">Optional organization name for path construction.</param>
    /// <returns>The full absolute path to the settings file.</returns>
    /// <remarks>
    /// If <paramref name="fileName"/> is an absolute path (rooted), it is returned as-is.
    /// Otherwise, it is combined with the base path resolved from the scope.
    /// </remarks>
    public static string GetFilePath(string fileName, SettingsScope scope, string? applicationName = null, string? organizationName = null)
    {
        if (Path.IsPathRooted(fileName))
        {
            return fileName;
        }

        string basePath = GetBasePath(scope, applicationName, organizationName);
        return Path.Combine(basePath, fileName);
    }

    /// <summary>
    /// Ensures that the directory containing the specified file path exists,
    /// creating it if necessary including any parent directories.
    /// </summary>
    /// <param name="filePath">The full path to a file. The directory portion of this path will be created if it doesn't exist.</param>
    /// <remarks>
    /// This method extracts the directory portion from the file path and creates it if it doesn't exist.
    /// If the directory already exists, this method does nothing. This is safe to call multiple times.
    /// </remarks>
    public static void EnsureDirectoryExists(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Gets the user-specific application data path for the current platform.
    /// </summary>
    /// <param name="appName">The application name to use in the path.</param>
    /// <param name="orgName">Optional organization name to use as a parent directory.</param>
    /// <returns>The full path to the user-specific application data directory.</returns>
    private static string GetUserPath(string appName, string? orgName)
    {
        // Handle Android first - uses internal app storage
        if (IsAndroid())
        {
            // On Android, AppContext.BaseDirectory points to the app's internal storage
            // which is the recommended location for app-specific files
            string basePath = AppContext.BaseDirectory;
            return BuildAppPath(basePath, appName, orgName);
        }

        // Handle iOS - uses Library directory within app sandbox
        if (IsIOS())
        {
            // On iOS, files should be stored in the Library directory
            // AppContext.BaseDirectory gives us the app bundle, so we go to Library
            string basePath = Path.Combine(AppContext.BaseDirectory, "Library");
            return BuildAppPath(basePath, appName, orgName);
        }

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (string.IsNullOrEmpty(appData))
        {
            // Fallback for Linux/macOS
            string home = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            appData = IsMacOS()
                ? Path.Combine(home, "Library", "Application Support")
                : Path.Combine(home, ".config");
        }

        return BuildAppPath(appData, appName, orgName);
    }

    /// <summary>
    /// Gets the machine-wide application data path for the current platform.
    /// </summary>
    /// <param name="appName">The application name to use in the path.</param>
    /// <param name="orgName">Optional organization name to use as a parent directory.</param>
    /// <returns>The full path to the machine-wide application data directory.</returns>
    /// <remarks>
    /// On mobile platforms (Android/iOS), machine-wide settings are not typically supported
    /// due to app sandboxing. In these cases, the method falls back to the user path.
    /// </remarks>
    private static string GetMachinePath(string appName, string? orgName)
    {
        // Mobile platforms don't have machine-wide settings due to app sandboxing
        // Fall back to user path behavior
        if (IsAndroid() || IsIOS())
        {
            return GetUserPath(appName, orgName);
        }

        string programData;

        if (IsWindows())
        {
            programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        }
        else if (IsMacOS())
        {
            programData = "/Library/Application Support";
        }
        else if (IsLinux())
        {
            programData = "/etc";
        }
        else
        {
            programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        }

        return BuildAppPath(programData, appName, orgName);
    }

    /// <summary>
    /// Gets the application's installation directory (where the executable is located).
    /// </summary>
    /// <returns>The full path to the application's directory.</returns>
    private static string GetApplicationPath()
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly?.Location is { Length: > 0 } location)
        {
            return Path.GetDirectoryName(location) ?? AppContext.BaseDirectory;
        }
        return AppContext.BaseDirectory;
    }

    /// <summary>
    /// Builds the full application path by combining base path, organization name, and application name.
    /// </summary>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="appName">The application name.</param>
    /// <param name="orgName">Optional organization name.</param>
    /// <returns>The combined path with sanitized directory names.</returns>
    private static string BuildAppPath(string basePath, string appName, string? orgName)
    {
        if (!string.IsNullOrEmpty(orgName))
        {
            return Path.Combine(basePath, SanitizeName(orgName!), SanitizeName(appName));
        }
        return Path.Combine(basePath, SanitizeName(appName));
    }

    /// <summary>
    /// Gets the default application name from the entry assembly, or "Sharingan" as fallback.
    /// </summary>
    /// <returns>The application name to use for path resolution.</returns>
    private static string GetDefaultApplicationName()
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        return entryAssembly?.GetName().Name ?? "Sharingan";
    }

    /// <summary>
    /// Sanitizes a name for use as a directory name by removing or replacing invalid file system characters.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <returns>A sanitized name safe for use as a directory name.</returns>
    private static string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "App";
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        string sanitized = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized;
    }

    /// <summary>
    /// Determines whether the current operating system is Windows.
    /// Uses platform-specific APIs based on the target framework.
    /// </summary>
    /// <returns><c>true</c> if running on Windows; otherwise, <c>false</c>.</returns>
    private static bool IsWindows() =>
#if NET5_0_OR_GREATER
        OperatingSystem.IsWindows();
#else
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif


    /// <summary>
    /// Determines whether the current operating system is Linux.
    /// Uses platform-specific APIs based on the target framework.
    /// </summary>
    /// <returns><c>true</c> if running on Linux; otherwise, <c>false</c>.</returns>
    private static bool IsLinux() =>
#if NET5_0_OR_GREATER
        OperatingSystem.IsLinux();
#else
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#endif


    /// <summary>
    /// Determines whether the current operating system is macOS.
    /// Uses platform-specific APIs based on the target framework.
    /// </summary>
    /// <returns><c>true</c> if running on macOS; otherwise, <c>false</c>.</returns>
    private static bool IsMacOS() =>
#if NET5_0_OR_GREATER
        OperatingSystem.IsMacOS();
#else
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#endif


    /// <summary>
    /// Determines whether the current operating system is Android.
    /// Uses platform-specific APIs based on the target framework.
    /// </summary>
    /// <returns><c>true</c> if running on Android; otherwise, <c>false</c>.</returns>
    private static bool IsAndroid() =>
#if NET5_0_OR_GREATER
        OperatingSystem.IsAndroid();
#else
        RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"));
#endif


    /// <summary>
    /// Determines whether the current operating system is iOS (or iPadOS/tvOS/watchOS).
    /// Uses platform-specific APIs based on the target framework.
    /// </summary>
    /// <returns><c>true</c> if running on iOS; otherwise, <c>false</c>.</returns>
    private static bool IsIOS() =>
#if NET5_0_OR_GREATER
        OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsWatchOS();
#else
        RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS"));
#endif

}
