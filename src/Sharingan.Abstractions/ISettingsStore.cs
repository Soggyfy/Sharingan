namespace Sharingan.Abstractions;

/// <summary>
/// Represents a store for key-value settings with both synchronous and asynchronous operations.
/// This interface serves as the core abstraction for all settings storage operations in the Sharingan library,
/// providing a unified API for persisting and retrieving application, user, and device settings.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ISettingsStore"/> interface provides comprehensive key-value storage capabilities
/// with support for both synchronous and asynchronous operations. Implementations of this interface
/// are expected to be thread-safe and handle concurrent access appropriately.
/// </para>
/// <para>
/// Settings are stored as typed values using generics, with automatic serialization and deserialization
/// handled by the underlying implementation. Complex objects are typically serialized to JSON format.
/// </para>
/// <para>
/// Implementations should implement <see cref="IDisposable"/> to properly release resources and
/// ensure any pending changes are flushed to the underlying storage before disposal.
/// </para>
/// </remarks>
/// <example>
/// Basic usage of ISettingsStore:
/// <code>
/// // Retrieve a setting with a default value
/// var theme = store.Get("app.theme", "dark");
/// 
/// // Store a setting
/// store.Set("app.lastOpened", DateTime.UtcNow);
/// 
/// // Check if a setting exists
/// if (store.ContainsKey("user.name"))
/// {
///     var name = store.GetOrDefault&lt;string&gt;("user.name");
/// }
/// 
/// // Persist changes to underlying storage
/// await store.FlushAsync();
/// </code>
/// </example>
public interface ISettingsStore : IDisposable
{
    #region Synchronous Read Operations

    /// <summary>
    /// Gets a value from the store with a required default value.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve. Can be a primitive type, string, or a complex object that can be deserialized from JSON.</typeparam>
    /// <param name="key">The unique key identifying the setting to retrieve. Keys are case-insensitive and support dot notation for hierarchical organization (e.g., "section.subsection.setting").</param>
    /// <param name="defaultValue">The default value to return if the key doesn't exist in the store or if the stored value cannot be converted to the specified type.</param>
    /// <returns>The stored value if the key exists and can be converted to type <typeparamref name="T"/>; otherwise, the <paramref name="defaultValue"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <example>
    /// <code>
    /// // Retrieve an integer setting
    /// int windowWidth = store.Get("window.width", 800);
    /// 
    /// // Retrieve a complex object
    /// var userPrefs = store.Get("user.preferences", new UserPreferences());
    /// </code>
    /// </example>
    T Get<T>(string key, T defaultValue);

    /// <summary>
    /// Gets a value from the store, returning null/default if not found.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve. Can be a primitive type, string, or a complex object that can be deserialized from JSON.</typeparam>
    /// <param name="key">The unique key identifying the setting to retrieve. Keys are case-insensitive and support dot notation for hierarchical organization.</param>
    /// <returns>The stored value if the key exists and can be converted to type <typeparamref name="T"/>; otherwise, the default value for type <typeparamref name="T"/> (null for reference types, zero/false for value types).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <example>
    /// <code>
    /// // Retrieve a nullable setting
    /// string? lastFile = store.GetOrDefault&lt;string&gt;("app.lastOpenedFile");
    /// if (lastFile != null)
    /// {
    ///     // Process the file
    /// }
    /// </code>
    /// </example>
    T? GetOrDefault<T>(string key);

    /// <summary>
    /// Attempts to get a value from the store using the try-pattern.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve. Can be a primitive type, string, or a complex object that can be deserialized from JSON.</typeparam>
    /// <param name="key">The unique key identifying the setting to retrieve. Keys are case-insensitive and support dot notation for hierarchical organization.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key if the key was found; otherwise, the default value for type <typeparamref name="T"/>. This parameter is passed uninitialized.</param>
    /// <returns><c>true</c> if the store contains a setting with the specified key; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <example>
    /// <code>
    /// if (store.TryGet&lt;int&gt;("window.height", out var height))
    /// {
    ///     Console.WriteLine($"Window height: {height}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Window height not configured");
    /// }
    /// </code>
    /// </example>
    bool TryGet<T>(string key, out T? value);

    /// <summary>
    /// Gets a value or creates it using the provided factory if it doesn't exist.
    /// This method is atomic and ensures the factory is only called once even under concurrent access.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve or create. Can be a primitive type, string, or a complex object that can be serialized to JSON.</typeparam>
    /// <param name="key">The unique key identifying the setting to retrieve or create. Keys are case-insensitive and support dot notation for hierarchical organization.</param>
    /// <param name="factory">A factory function that creates the default value if the key doesn't exist. The factory is only invoked if the key is not found.</param>
    /// <returns>The existing value if the key was found; otherwise, the newly created value from the factory.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="factory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <remarks>
    /// This method provides a convenient way to ensure a setting always has a value, creating it on first access.
    /// The newly created value is automatically stored in the settings store.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get or create an application configuration
    /// var config = store.GetOrCreate("app.config", () => new AppConfiguration
    /// {
    ///     Theme = "dark",
    ///     Language = "en"
    /// });
    /// </code>
    /// </example>
    T GetOrCreate<T>(string key, Func<T> factory);

    #endregion

    #region Synchronous Write Operations

    /// <summary>
    /// Sets a value in the store, creating the key if it doesn't exist or updating it if it does.
    /// </summary>
    /// <typeparam name="T">The type of the value to store. Can be a primitive type, string, or a complex object that can be serialized to JSON.</typeparam>
    /// <param name="key">The unique key identifying the setting to set. Keys are case-insensitive and support dot notation for hierarchical organization.</param>
    /// <param name="value">The value to store. Complex objects will be serialized using the configured serializer (typically JSON).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the store is read-only.</exception>
    /// <remarks>
    /// Changes may be cached in memory and require calling <see cref="Flush"/> or <see cref="FlushAsync"/>
    /// to persist them to the underlying storage, depending on the implementation.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Store a simple value
    /// store.Set("app.volume", 75);
    /// 
    /// // Store a complex object
    /// store.Set("user.settings", new UserSettings { Theme = "dark" });
    /// </code>
    /// </example>
    void Set<T>(string key, T value);

    /// <summary>
    /// Removes a value from the store.
    /// </summary>
    /// <param name="key">The unique key identifying the setting to remove. Keys are case-insensitive.</param>
    /// <returns><c>true</c> if the setting was found and removed; <c>false</c> if the key was not found in the store.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the store is read-only.</exception>
    /// <example>
    /// <code>
    /// if (store.Remove("temp.cachedData"))
    /// {
    ///     Console.WriteLine("Cached data was removed");
    /// }
    /// </code>
    /// </example>
    bool Remove(string key);

    /// <summary>
    /// Clears all settings from the store, removing every key-value pair.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the store is read-only.</exception>
    /// <remarks>
    /// This operation is irreversible. After calling <see cref="Clear"/>, the store will be empty.
    /// Changes may require calling <see cref="Flush"/> to persist them to the underlying storage.
    /// </remarks>
    void Clear();

    #endregion

    #region Query Operations

    /// <summary>
    /// Checks if a key exists in the store.
    /// </summary>
    /// <param name="key">The unique key to check for existence. Keys are case-insensitive.</param>
    /// <returns><c>true</c> if the store contains a setting with the specified key; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <example>
    /// <code>
    /// if (store.ContainsKey("user.profile"))
    /// {
    ///     var profile = store.GetOrDefault&lt;UserProfile&gt;("user.profile");
    /// }
    /// </code>
    /// </example>
    bool ContainsKey(string key);

    /// <summary>
    /// Gets all keys currently stored in the settings store.
    /// </summary>
    /// <returns>An enumerable collection of all unique keys in the store. Returns an empty collection if the store is empty.</returns>
    /// <remarks>
    /// The returned keys may include hierarchical keys with dot notation (e.g., "section.key").
    /// The order of keys is implementation-dependent and should not be relied upon.
    /// </remarks>
    /// <example>
    /// <code>
    /// foreach (var key in store.GetAllKeys())
    /// {
    ///     Console.WriteLine($"Key: {key}");
    /// }
    /// </code>
    /// </example>
    IEnumerable<string> GetAllKeys();

    /// <summary>
    /// Gets the number of settings currently stored in the settings store.
    /// </summary>
    /// <value>The total count of key-value pairs in the store. Returns 0 if the store is empty.</value>
    int Count { get; }

    #endregion

    #region Persistence Operations

    /// <summary>
    /// Flushes pending changes to the underlying storage synchronously.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method ensures that all in-memory changes are written to the underlying persistent storage
    /// (e.g., file system, database, registry). For implementations that use caching, this method
    /// guarantees data durability.
    /// </para>
    /// <para>
    /// For implementations that write through immediately (e.g., registry provider), this method may be a no-op.
    /// </para>
    /// </remarks>
    /// <exception cref="IOException">Thrown when an I/O error occurs while writing to the underlying storage.</exception>
    void Flush();

    /// <summary>
    /// Asynchronously flushes pending changes to the underlying storage.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the flush operation. If cancellation is requested, the operation may leave partial data unflushed.</param>
    /// <returns>A task representing the asynchronous flush operation.</returns>
    /// <remarks>
    /// <para>
    /// This method ensures that all in-memory changes are written to the underlying persistent storage
    /// (e.g., file system, database, registry). For implementations that use caching, this method
    /// guarantees data durability.
    /// </para>
    /// <para>
    /// For implementations that write through immediately (e.g., registry provider), this method may complete immediately.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while writing to the underlying storage.</exception>
    Task FlushAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Asynchronous Operations

    /// <summary>
    /// Asynchronously gets a value from the store with a required default value.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve. Can be a primitive type, string, or a complex object that can be deserialized from JSON.</typeparam>
    /// <param name="key">The unique key identifying the setting to retrieve. Keys are case-insensitive and support dot notation for hierarchical organization.</param>
    /// <param name="defaultValue">The default value to return if the key doesn't exist in the store or if the stored value cannot be converted to the specified type.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the stored value if the key exists; otherwise, the <paramref name="defaultValue"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a value from the store, returning null/default if not found.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve. Can be a primitive type, string, or a complex object that can be deserialized from JSON.</typeparam>
    /// <param name="key">The unique key identifying the setting to retrieve. Keys are case-insensitive and support dot notation for hierarchical organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the stored value if the key exists; otherwise, the default value for type <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    Task<T?> GetOrDefaultAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a value or creates it using the provided factory if it doesn't exist.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve or create. Can be a primitive type, string, or a complex object that can be serialized to JSON.</typeparam>
    /// <param name="key">The unique key identifying the setting to retrieve or create. Keys are case-insensitive and support dot notation for hierarchical organization.</param>
    /// <param name="factory">A factory function that creates the default value if the key doesn't exist. The factory is only invoked if the key is not found.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the existing value if the key was found; otherwise, the newly created value from the factory.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="factory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    Task<T> GetOrCreateAsync<T>(string key, Func<T> factory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a value or creates it using the provided async factory if it doesn't exist.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve or create. Can be a primitive type, string, or a complex object that can be serialized to JSON.</typeparam>
    /// <param name="key">The unique key identifying the setting to retrieve or create. Keys are case-insensitive and support dot notation for hierarchical organization.</param>
    /// <param name="factory">An asynchronous factory function that creates the default value if the key doesn't exist. The factory is only invoked if the key is not found, and receives a cancellation token.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation. This token is passed to the factory function.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the existing value if the key was found; otherwise, the newly created value from the factory.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="factory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    /// <remarks>
    /// This overload is useful when the factory operation itself is asynchronous, such as fetching a default value from a remote service or performing async initialization.
    /// </remarks>
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sets a value in the store, creating the key if it doesn't exist or updating it if it does.
    /// </summary>
    /// <typeparam name="T">The type of the value to store. Can be a primitive type, string, or a complex object that can be serialized to JSON.</typeparam>
    /// <param name="key">The unique key identifying the setting to set. Keys are case-insensitive and support dot notation for hierarchical organization.</param>
    /// <param name="value">The value to store. Complex objects will be serialized using the configured serializer (typically JSON).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the store is read-only.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously removes a value from the store.
    /// </summary>
    /// <param name="key">The unique key identifying the setting to remove. Keys are case-insensitive.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result is <c>true</c> if the setting was found and removed; <c>false</c> if the key was not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the store is read-only.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously clears all settings from the store, removing every key-value pair.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the store is read-only.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    /// <remarks>
    /// This operation is irreversible. After this method completes, the store will be empty.
    /// </remarks>
    Task ClearAsync(CancellationToken cancellationToken = default);

    #endregion
}
