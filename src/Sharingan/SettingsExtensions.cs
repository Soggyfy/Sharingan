using Sharingan.Abstractions;

namespace Sharingan;

/// <summary>
/// Provides extension methods for <see cref="ISettingsStore"/> that add type-safe getters,
/// strongly-typed settings support, change notifications, and utility methods for common operations.
/// </summary>
/// <remarks>
/// <para>
/// This class extends the base <see cref="ISettingsStore"/> interface with convenient methods for:
/// <list type="bullet">
/// <item><description>Type-safe primitive type getters (string, int, bool, etc.)</description></item>
/// <item><description>Strongly-typed settings objects using the Options pattern</description></item>
/// <item><description>Change notification subscriptions for reactive programming</description></item>
/// <item><description>Batch operations like removing multiple keys at once</description></item>
/// </list>
/// </para>
/// <para>
/// All extension methods are thread-safe when used with thread-safe store implementations.
/// </para>
/// </remarks>
/// <example>
/// Using type-safe getters:
/// <code>
/// var theme = store.GetString("ui.theme", "dark");
/// var volume = store.GetInt("audio.volume", 75);
/// var muted = store.GetBool("audio.muted", false);
/// </code>
/// </example>
/// <example>
/// Using strongly-typed settings:
/// <code>
/// // Get settings object (creates if not exists)
/// var userPrefs = store.GetSettings&lt;UserPreferences&gt;();
/// 
/// // Modify and save
/// userPrefs.Theme = "light";
/// store.SaveSettings(userPrefs);
/// </code>
/// </example>
/// <seealso cref="ISettingsStore"/>
/// <seealso cref="IObservableSettingsStore"/>
public static class SettingsExtensions
{
    #region Type-Safe Getters

    /// <summary>
    /// Gets a string value from the store with an optional default value.
    /// </summary>
    /// <param name="store">The settings store to read from.</param>
    /// <param name="key">The unique key identifying the setting to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the key doesn't exist. Default is an empty string.</param>
    /// <returns>The stored string value, or <paramref name="defaultValue"/> if the key doesn't exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> or <paramref name="key"/> is null.</exception>
    public static string GetString(this ISettingsStore store, string key, string defaultValue = "")
    {
        return store.Get(key, defaultValue);
    }

    /// <summary>
    /// Gets an integer value from the store with an optional default value.
    /// </summary>
    /// <param name="store">The settings store to read from.</param>
    /// <param name="key">The unique key identifying the setting to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the key doesn't exist or cannot be converted. Default is 0.</param>
    /// <returns>The stored integer value, or <paramref name="defaultValue"/> if the key doesn't exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> or <paramref name="key"/> is null.</exception>
    public static int GetInt(this ISettingsStore store, string key, int defaultValue = 0)
    {
        return store.Get(key, defaultValue);
    }

    /// <summary>
    /// Gets a long integer value from the store with an optional default value.
    /// </summary>
    /// <param name="store">The settings store to read from.</param>
    /// <param name="key">The unique key identifying the setting to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the key doesn't exist or cannot be converted. Default is 0.</param>
    /// <returns>The stored long value, or <paramref name="defaultValue"/> if the key doesn't exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> or <paramref name="key"/> is null.</exception>
    public static long GetLong(this ISettingsStore store, string key, long defaultValue = 0)
    {
        return store.Get(key, defaultValue);
    }

    /// <summary>
    /// Gets a boolean value from the store with an optional default value.
    /// </summary>
    /// <param name="store">The settings store to read from.</param>
    /// <param name="key">The unique key identifying the setting to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the key doesn't exist or cannot be converted. Default is false.</param>
    /// <returns>The stored boolean value, or <paramref name="defaultValue"/> if the key doesn't exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> or <paramref name="key"/> is null.</exception>
    public static bool GetBool(this ISettingsStore store, string key, bool defaultValue = false)
    {
        return store.Get(key, defaultValue);
    }

    /// <summary>
    /// Gets a double value from the store.
    /// </summary>
    /// <param name="store">The settings store.</param>
    /// <param name="key">The key to retrieve.</param>
    /// <param name="defaultValue">The default value if the key doesn't exist.</param>
    /// <returns>The stored value or the default value.</returns>
    public static double GetDouble(this ISettingsStore store, string key, double defaultValue = 0.0)
    {
        return store.Get(key, defaultValue);
    }

    /// <summary>
    /// Gets a decimal value from the store.
    /// </summary>
    /// <param name="store">The settings store.</param>
    /// <param name="key">The key to retrieve.</param>
    /// <param name="defaultValue">The default value if the key doesn't exist.</param>
    /// <returns>The stored value or the default value.</returns>
    public static decimal GetDecimal(this ISettingsStore store, string key, decimal defaultValue = 0m)
    {
        return store.Get(key, defaultValue);
    }

    /// <summary>
    /// Gets a DateTime value from the store.
    /// </summary>
    /// <param name="store">The settings store.</param>
    /// <param name="key">The key to retrieve.</param>
    /// <param name="defaultValue">The default value if the key doesn't exist.</param>
    /// <returns>The stored value or the default value.</returns>
    public static DateTime GetDateTime(this ISettingsStore store, string key, DateTime defaultValue = default)
    {
        return store.Get(key, defaultValue);
    }

    /// <summary>
    /// Gets a Guid value from the store.
    /// </summary>
    /// <param name="store">The settings store.</param>
    /// <param name="key">The key to retrieve.</param>
    /// <param name="defaultValue">The default value if the key doesn't exist.</param>
    /// <returns>The stored value or the default value.</returns>
    public static Guid GetGuid(this ISettingsStore store, string key, Guid defaultValue = default)
    {
        return store.Get(key, defaultValue);
    }

    /// <summary>
    /// Gets a TimeSpan value from the store.
    /// </summary>
    /// <param name="store">The settings store.</param>
    /// <param name="key">The key to retrieve.</param>
    /// <param name="defaultValue">The default value if the key doesn't exist.</param>
    /// <returns>The stored value or the default value.</returns>
    public static TimeSpan GetTimeSpan(this ISettingsStore store, string key, TimeSpan defaultValue = default)
    {
        return store.Get(key, defaultValue);
    }

    #endregion

    #region Strongly-Typed Settings

    /// <summary>
    /// Gets a strongly-typed settings object from the store.
    /// The type name is used as the key.
    /// </summary>
    /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="store">The settings store.</param>
    /// <returns>The settings object or a new instance if not found.</returns>
    public static T GetSettings<T>(this ISettingsStore store) where T : class, new()
    {
        string key = typeof(T).Name;
        return store.Get(key, new T());
    }

    /// <summary>
    /// Gets a strongly-typed settings object from the store with a custom key.
    /// </summary>
    /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="store">The settings store.</param>
    /// <param name="key">The key to use.</param>
    /// <returns>The settings object or a new instance if not found.</returns>
    public static T GetSettings<T>(this ISettingsStore store, string key) where T : class, new()
    {
        return store.Get(key, new T());
    }

    /// <summary>
    /// Saves a strongly-typed settings object to the store.
    /// The type name is used as the key.
    /// </summary>
    /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="store">The settings store.</param>
    /// <param name="settings">The settings to save.</param>
    public static void SaveSettings<T>(this ISettingsStore store, T settings) where T : class
    {
        string key = typeof(T).Name;
        store.Set(key, settings);
    }

    /// <summary>
    /// Saves a strongly-typed settings object to the store with a custom key.
    /// </summary>
    /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="store">The settings store.</param>
    /// <param name="key">The key to use.</param>
    /// <param name="settings">The settings to save.</param>
    public static void SaveSettings<T>(this ISettingsStore store, string key, T settings) where T : class
    {
        store.Set(key, settings);
    }

    /// <summary>
    /// Asynchronously gets a strongly-typed settings object from the store.
    /// </summary>
    /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="store">The settings store.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>The settings object or a new instance if not found.</returns>
    public static Task<T> GetSettingsAsync<T>(this ISettingsStore store, CancellationToken cancellationToken = default) where T : class, new()
    {
        string key = typeof(T).Name;
        return store.GetAsync(key, new T(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously saves a strongly-typed settings object to the store.
    /// </summary>
    /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="store">The settings store.</param>
    /// <param name="settings">The settings to save.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task representing the operation.</returns>
    public static Task SaveSettingsAsync<T>(this ISettingsStore store, T settings, CancellationToken cancellationToken = default) where T : class
    {
        string key = typeof(T).Name;
        return store.SetAsync(key, settings, cancellationToken);
    }

    #endregion

    #region Change Notifications

    /// <summary>
    /// Subscribes to changes for a specific key.
    /// </summary>
    /// <param name="store">The settings store (must implement <see cref="IObservableSettingsStore"/>).</param>
    /// <param name="key">The key to watch.</param>
    /// <param name="handler">The handler to invoke when the key changes.</param>
    /// <returns>A disposable subscription that can be used to unsubscribe.</returns>
    /// <exception cref="NotSupportedException">Thrown if the store doesn't support change notifications.</exception>
    public static IDisposable OnChange(this ISettingsStore store, string key, Action<SettingsChangedEventArgs> handler)
    {
        if (store is not IObservableSettingsStore observable)
        {
            throw new NotSupportedException("This store does not support change notifications.");
        }

        void Handler(object? sender, SettingsChangedEventArgs e)
        {
            if (string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase) ||
                e.ChangeType == SettingsChangeType.Cleared)
            {
                handler(e);
            }
        }

        observable.SettingsChanged += Handler;
        return new ChangeSubscription(() => observable.SettingsChanged -= Handler);
    }

    /// <summary>
    /// Subscribes to all settings changes.
    /// </summary>
    /// <param name="store">The settings store (must implement <see cref="IObservableSettingsStore"/>).</param>
    /// <param name="handler">The handler to invoke when any setting changes.</param>
    /// <returns>A disposable subscription that can be used to unsubscribe.</returns>
    /// <exception cref="NotSupportedException">Thrown if the store doesn't support change notifications.</exception>
    public static IDisposable OnAnyChange(this ISettingsStore store, Action<SettingsChangedEventArgs> handler)
    {
        if (store is not IObservableSettingsStore observable)
        {
            throw new NotSupportedException("This store does not support change notifications.");
        }

        void Handler(object? sender, SettingsChangedEventArgs e)
        {
            handler(e);
        }

        observable.SettingsChanged += Handler;
        return new ChangeSubscription(() => observable.SettingsChanged -= Handler);
    }

    private sealed class ChangeSubscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose()
        {
            Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets a value or sets it using the provided default if it doesn't exist.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="store">The settings store.</param>
    /// <param name="key">The key to get or set.</param>
    /// <param name="defaultValue">The default value to set if the key doesn't exist.</param>
    /// <returns>The existing or newly set value.</returns>
    public static T GetOrSet<T>(this ISettingsStore store, string key, T defaultValue)
    {
        return store.GetOrCreate(key, () => defaultValue);
    }

    /// <summary>
    /// Removes multiple keys from the store.
    /// </summary>
    /// <param name="store">The settings store.</param>
    /// <param name="keys">The keys to remove.</param>
    /// <returns>The number of keys that were removed.</returns>
    public static int RemoveRange(this ISettingsStore store, IEnumerable<string> keys)
    {
        int count = 0;
        foreach (string key in keys)
        {
            if (store.Remove(key))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Removes all keys that match the specified prefix.
    /// </summary>
    /// <param name="store">The settings store.</param>
    /// <param name="prefix">The prefix to match.</param>
    /// <returns>The number of keys that were removed.</returns>
    public static int RemoveByPrefix(this ISettingsStore store, string prefix)
    {
        List<string> keysToRemove = store.GetAllKeys()
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return store.RemoveRange(keysToRemove);
    }

    /// <summary>
    /// Gets all keys that match the specified prefix.
    /// </summary>
    /// <param name="store">The settings store.</param>
    /// <param name="prefix">The prefix to match.</param>
    /// <returns>An enumerable of matching keys.</returns>
    public static IEnumerable<string> GetKeysByPrefix(this ISettingsStore store, string prefix)
    {
        return store.GetAllKeys()
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
