using Microsoft.Data.Sqlite;
using Sharingan.Abstractions;
using Sharingan.Internal;
using Sharingan.Serialization;
using System.Collections.Concurrent;

namespace Sharingan.Providers.SQLite;

/// <summary>
/// A SQLite database-based settings provider for robust, ACID-compliant settings storage.
/// Ideal for applications requiring reliable persistence and support for many settings.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SQLiteSettingsProvider"/> stores settings in a local SQLite database file,
/// providing enterprise-grade features:
/// <list type="bullet">
/// <item><description>ACID compliance: Guaranteed data integrity even during crashes</description></item>
/// <item><description>High performance: Efficient for large numbers of settings</description></item>
/// <item><description>Built-in indexing: Fast key lookups</description></item>
/// <item><description>Cross-platform: Works on Windows, Linux, and macOS</description></item>
/// <item><description>Concurrent access: Connection pooling with shared cache</description></item>
/// </list>
/// </para>
/// <para>
/// Settings are stored in a table with columns for Key (PRIMARY KEY), Value, and UpdatedAt.
/// Complex objects are serialized to JSON strings.
/// </para>
/// </remarks>
/// <example>
/// Using the SQLite provider:
/// <code>
/// var provider = new SQLiteSettingsProvider(new SQLiteProviderOptions
/// {
///     DatabasePath = "app-settings.db",
///     TableName = "Settings"
/// });
/// 
/// provider.Set("app.theme", "dark");
/// provider.Set("app.lastRun", DateTime.UtcNow);
/// provider.Flush(); // Commit to database
/// </code>
/// </example>
/// <seealso cref="SQLiteProviderOptions"/>
/// <seealso cref="SharinganBuilderExtensions.UseSQLite"/>
public class SQLiteSettingsProvider : ISettingsProvider
{
    /// <summary>
    /// Thread-safe cache of settings loaded from the database.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Configuration options for this provider.
    /// </summary>
    private readonly SQLiteProviderOptions _options;

    /// <summary>
    /// Serializer for converting typed values to strings.
    /// </summary>
    private readonly ISettingsSerializer _serializer;

    /// <summary>
    /// The SQLite connection string.
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    /// Tracks whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

#pragma warning disable CS0414
    /// <summary>
    /// Tracks whether there are unsaved changes in the cache.
    /// </summary>
    private bool _isDirty;
#pragma warning restore CS0414

    /// <inheritdoc />
    /// <value>Returns "SQLite:{filename}" for identification.</value>
    public string Name => $"SQLite:{Path.GetFileName(_options.DatabasePath)}";

    /// <inheritdoc />
    /// <value>Always returns <c>false</c> since SQLite databases are writable.</value>
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public int Priority => _options.Priority;

    /// <inheritdoc />
    public SettingsScope Scope => _options.Scope;

    /// <inheritdoc />
    public int Count => _cache.Count;

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteSettingsProvider"/> class.
    /// </summary>
    /// <param name="options">Configuration options for database path, table name, and scope. If null, default options are used.</param>
    /// <param name="serializer">Optional custom serializer. If null, uses the default JSON serializer.</param>
    /// <remarks>
    /// The constructor creates the database file and settings table if they don't exist
    /// (when <see cref="SQLiteProviderOptions.CreateIfNotExists"/> is true).
    /// </remarks>
    public SQLiteSettingsProvider(SQLiteProviderOptions? options = null, ISettingsSerializer? serializer = null)
    {
        _options = options ?? new SQLiteProviderOptions();
        _serializer = serializer ?? _options.Serializer ?? JsonSettingsSerializer.Default;

        string dbPath = PathResolver.GetFilePath(_options.DatabasePath, _options.Scope, _options.ApplicationName, _options.OrganizationName);
        _connectionString = $"Data Source={dbPath};Pooling=true;Cache=Shared";

        if (_options.CreateIfNotExists)
        {
            PathResolver.EnsureDirectoryExists(dbPath);
            EnsureTableExists();
        }

        LoadFromDatabase();
    }

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        if (_cache.TryGetValue(key, out string? value))
        {
            return ConvertValue<T>(value, defaultValue);
        }
        return defaultValue;
    }

    /// <inheritdoc />
    public T? GetOrDefault<T>(string key)
    {
        return Get<T>(key, default!);
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T? value)
    {
        if (_cache.TryGetValue(key, out string? stored))
        {
            value = ConvertValue<T>(stored, default);
            return true;
        }
        value = default;
        return false;
    }

    /// <inheritdoc />
    public T GetOrCreate<T>(string key, Func<T> factory)
    {
        if (TryGet<T>(key, out T? existing) && existing is not null)
        {
            return existing;
        }

        T? value = factory();
        Set(key, value);
        return value;
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value)
    {
        string stringValue = ConvertToString(value);
        _cache[key] = stringValue;
        _isDirty = true;

        // Write-through to database
        WriteToDatabase(key, stringValue);

        OnSettingsChanged(new SettingsChangedEventArgs(key, SettingsChangeType.Modified, null, value, Name));
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        if (_cache.TryRemove(key, out _))
        {
            _isDirty = true;
            DeleteFromDatabase(key);
            OnSettingsChanged(new SettingsChangedEventArgs(key, SettingsChangeType.Removed, null, null, Name));
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _cache.Clear();
        _isDirty = true;
        ClearDatabase();
        OnSettingsChanged(SettingsChangedEventArgs.Cleared(Name));
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        return _cache.ContainsKey(key);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllKeys()
    {
        return _cache.Keys;
    }

    /// <inheritdoc />
    public void Flush() { /* SQLite uses write-through */ }
    /// <inheritdoc />
    public void Reload()
    {
        LoadFromDatabase();
    }

    public Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken ct = default)
    {
        return Task.FromResult(Get(key, defaultValue));
    }

    public Task<T?> GetOrDefaultAsync<T>(string key, CancellationToken ct = default)
    {
        return Task.FromResult(GetOrDefault<T>(key));
    }

    public Task<T> GetOrCreateAsync<T>(string key, Func<T> factory, CancellationToken ct = default)
    {
        return Task.FromResult(GetOrCreate(key, factory));
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
    {
        if (TryGet<T>(key, out T? existing) && existing is not null)
        {
            return existing;
        }

        T? value = await factory(ct).ConfigureAwait(false);
        Set(key, value);
        return value;
    }
    public Task SetAsync<T>(string key, T value, CancellationToken ct = default) { Set(key, value); return Task.CompletedTask; }
    public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(Remove(key));
    }

    public Task ClearAsync(CancellationToken ct = default) { Clear(); return Task.CompletedTask; }
    public Task FlushAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task ReloadAsync(CancellationToken ct = default) { Reload(); return Task.CompletedTask; }

    private void EnsureTableExists()
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {_options.TableName} (
                Key TEXT PRIMARY KEY NOT NULL,
                Value TEXT,
                UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP
            )";
        command.ExecuteNonQuery();
    }

    private void LoadFromDatabase()
    {
        _cache.Clear();

        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT Key, Value FROM {_options.TableName}";

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            string key = reader.GetString(0);
            string value = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            _cache[key] = value;
        }
        _isDirty = false;
    }

    private void WriteToDatabase(string key, string value)
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $@"
            INSERT INTO {_options.TableName} (Key, Value, UpdatedAt)
            VALUES (@key, @value, CURRENT_TIMESTAMP)
            ON CONFLICT(Key) DO UPDATE SET Value = @value, UpdatedAt = CURRENT_TIMESTAMP";
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);
        command.ExecuteNonQuery();
    }

    private void DeleteFromDatabase(string key)
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {_options.TableName} WHERE Key = @key";
        command.Parameters.AddWithValue("@key", key);
        command.ExecuteNonQuery();
    }

    private void ClearDatabase()
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {_options.TableName}";
        command.ExecuteNonQuery();
    }

    private T? ConvertValue<T>(string value, T? defaultValue)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }

        if (typeof(T) == typeof(int) && int.TryParse(value, out int i))
        {
            return (T)(object)i;
        }

        if (typeof(T) == typeof(long) && long.TryParse(value, out long l))
        {
            return (T)(object)l;
        }

        if (typeof(T) == typeof(bool) && bool.TryParse(value, out bool b))
        {
            return (T)(object)b;
        }

        if (typeof(T) == typeof(double) && double.TryParse(value, out double d))
        {
            return (T)(object)d;
        }

        try { return _serializer.Deserialize<T>(value) ?? defaultValue; } catch { return defaultValue; }
    }

    private string ConvertToString<T>(T value)
    {
        if (value is string s)
        {
            return s;
        }

        if (value is int or long or bool or double or float or decimal)
        {
            return value.ToString() ?? "";
        }

        return _serializer.Serialize(value);
    }

    protected virtual void OnSettingsChanged(SettingsChangedEventArgs e)
    {
        SettingsChanged?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            // Clear the connection pool to release file locks
            using SqliteConnection connection = new(_connectionString);
            SqliteConnection.ClearPool(connection);
        }
        GC.SuppressFinalize(this);
    }
}
