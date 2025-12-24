using Sharingan.Abstractions;

namespace Sharingan.Providers.SQLite;

/// <summary>
/// Extension methods for <see cref="SharinganBuilder"/> to add SQLite database provider support.
/// </summary>
/// <remarks>
/// SQLite provides a robust, embedded database solution for settings that require:
/// ACID compliance, efficient storage for many settings, or structured query capabilities.
/// </remarks>
/// <seealso cref="SQLiteSettingsProvider"/>
/// <seealso cref="SQLiteProviderOptions"/>
public static class SharinganBuilderExtensions
{
    /// <summary>
    /// Adds a SQLite database provider to the settings store configuration.
    /// Settings are stored in a local SQLite database file.
    /// </summary>
    /// <param name="builder">The Sharingan builder instance.</param>
    /// <param name="databasePath">The path to the SQLite database file. Can be relative or absolute. Default is "settings.db".</param>
    /// <param name="scope">The settings scope determining where the file is stored. Default is <see cref="SettingsScope.User"/>.</param>
    /// <param name="priority">The provider priority for composite stores. Default is 0.</param>
    /// <param name="configure">Optional action to configure additional SQLite provider options.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var store = new SharinganBuilder()
    ///     .UseSQLite("app-config.db", SettingsScope.User, configure: options =>
    ///     {
    ///         options.TableName = "AppSettings";
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    public static SharinganBuilder UseSQLite(
        this SharinganBuilder builder,
        string databasePath = "settings.db",
        SettingsScope scope = SettingsScope.User,
        int priority = 0,
        Action<SQLiteProviderOptions>? configure = null)
    {
        SQLiteProviderOptions options = new()
        {
            DatabasePath = databasePath,
            Scope = scope,
            Priority = priority
        };
        configure?.Invoke(options);
        return builder.UseProvider(new SQLiteSettingsProvider(options));
    }
}
