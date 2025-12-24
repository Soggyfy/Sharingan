using Sharingan.Abstractions;

namespace Sharingan.Providers.SQLite;

/// <summary>
/// Configuration options for <see cref="SQLiteSettingsProvider"/>, extending the base
/// <see cref="SettingsProviderOptions"/> with SQLite database-specific settings.
/// </summary>
/// <remarks>
/// <para>
/// SQLite provides a robust, file-based database solution for settings storage, offering:
/// <list type="bullet">
/// <item><description>ACID compliance for reliable data persistence</description></item>
/// <item><description>Support for large numbers of settings efficiently</description></item>
/// <item><description>Built-in data integrity and corruption recovery</description></item>
/// <item><description>Cross-platform support (Windows, Linux, macOS)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new SQLiteProviderOptions
/// {
///     DatabasePath = "app-settings.db",
///     TableName = "Configuration",
///     Scope = SettingsScope.User
/// };
/// </code>
/// </example>
/// <seealso cref="SQLiteSettingsProvider"/>
public class SQLiteProviderOptions : SettingsProviderOptions
{
    /// <summary>
    /// Gets or sets the database file path for the SQLite database.
    /// Can be relative (resolved based on scope) or absolute.
    /// </summary>
    /// <value>The path to the SQLite database file. Default is "settings.db".</value>
    public string DatabasePath { get; set; } = "settings.db";

    /// <summary>
    /// Gets or sets the table name for storing settings key-value pairs.
    /// </summary>
    /// <value>The table name to use. Default is "Settings".</value>
    /// <remarks>
    /// The table is created automatically with columns: Key (PRIMARY KEY), Value, UpdatedAt.
    /// </remarks>
    public string TableName { get; set; } = "Settings";

    /// <summary>
    /// Gets or sets whether to create the database file and table if they don't exist.
    /// </summary>
    /// <value><c>true</c> to create automatically; <c>false</c> to throw if missing. Default is <c>true</c>.</value>
    public bool CreateIfNotExists { get; set; } = true;

    /// <summary>
    /// Gets or sets the connection pool size for database connections.
    /// </summary>
    /// <value>The pool size. Default is 1 (single connection with shared cache).</value>
    public int PoolSize { get; set; } = 1;
}
