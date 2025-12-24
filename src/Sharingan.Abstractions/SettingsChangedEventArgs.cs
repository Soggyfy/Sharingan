namespace Sharingan.Abstractions;

/// <summary>
/// Specifies the type of change that occurred to a setting.
/// Used by <see cref="SettingsChangedEventArgs"/> to describe the nature of a settings modification.
/// </summary>
/// <remarks>
/// This enumeration is used in the settings change notification system to indicate what kind of
/// modification was made to a setting, allowing event handlers to respond appropriately based on
/// the type of change.
/// </remarks>
/// <seealso cref="SettingsChangedEventArgs"/>
/// <seealso cref="IObservableSettingsStore"/>
/// <seealso cref="ISettingsProvider"/>
public enum SettingsChangeType
{
    /// <summary>
    /// Indicates that a new setting was added to the store.
    /// This occurs when <see cref="ISettingsStore.Set{T}"/> or <see cref="ISettingsStore.GetOrCreate{T}"/>
    /// is called for a key that did not previously exist in the store.
    /// </summary>
    /// <remarks>
    /// When this change type is reported, <see cref="SettingsChangedEventArgs.OldValue"/> will be null,
    /// and <see cref="SettingsChangedEventArgs.NewValue"/> will contain the newly added value.
    /// </remarks>
    Added,

    /// <summary>
    /// Indicates that an existing setting was modified (its value was changed).
    /// This occurs when <see cref="ISettingsStore.Set{T}"/> is called for a key that already
    /// exists in the store with a different value.
    /// </summary>
    /// <remarks>
    /// When this change type is reported, <see cref="SettingsChangedEventArgs.OldValue"/> will contain
    /// the previous value, and <see cref="SettingsChangedEventArgs.NewValue"/> will contain the updated value.
    /// </remarks>
    Modified,

    /// <summary>
    /// Indicates that a setting was removed from the store.
    /// This occurs when <see cref="ISettingsStore.Remove"/> or <see cref="ISettingsStore.RemoveAsync"/>
    /// is called successfully for an existing key.
    /// </summary>
    /// <remarks>
    /// When this change type is reported, <see cref="SettingsChangedEventArgs.OldValue"/> will contain
    /// the removed value, and <see cref="SettingsChangedEventArgs.NewValue"/> will be null.
    /// </remarks>
    Removed,

    /// <summary>
    /// Indicates that all settings were cleared from the store.
    /// This occurs when <see cref="ISettingsStore.Clear"/> or <see cref="ISettingsStore.ClearAsync"/>
    /// is called.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this change type is reported, <see cref="SettingsChangedEventArgs.Key"/> will typically
    /// be an empty string, and both <see cref="SettingsChangedEventArgs.OldValue"/> and
    /// <see cref="SettingsChangedEventArgs.NewValue"/> will be null.
    /// </para>
    /// <para>
    /// This represents a bulk operation where all keys are removed simultaneously.
    /// Handlers should treat this as if every setting in the store was removed.
    /// </para>
    /// </remarks>
    Cleared
}

/// <summary>
/// Provides data for settings change events, containing detailed information about what changed,
/// the old and new values, and when the change occurred.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SettingsChangedEventArgs"/> is used by <see cref="IObservableSettingsStore"/> and
/// <see cref="ISettingsProvider"/> to communicate details about settings modifications to subscribers.
/// </para>
/// <para>
/// The event args include:
/// <list type="bullet">
/// <item><description>The key that was affected</description></item>
/// <item><description>The type of change (added, modified, removed, or cleared)</description></item>
/// <item><description>The old and new values (when applicable)</description></item>
/// <item><description>The provider that raised the event</description></item>
/// <item><description>A timestamp indicating when the change occurred</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Handling settings change events:
/// <code>
/// observable.SettingsChanged += (sender, args) =>
/// {
///     switch (args.ChangeType)
///     {
///         case SettingsChangeType.Added:
///             Console.WriteLine($"New setting: {args.Key} = {args.NewValue}");
///             break;
///         case SettingsChangeType.Modified:
///             Console.WriteLine($"Changed: {args.Key} from {args.OldValue} to {args.NewValue}");
///             break;
///         case SettingsChangeType.Removed:
///             Console.WriteLine($"Removed: {args.Key} (was {args.OldValue})");
///             break;
///         case SettingsChangeType.Cleared:
///             Console.WriteLine("All settings cleared!");
///             break;
///     }
/// };
/// </code>
/// </example>
/// <seealso cref="SettingsChangeType"/>
/// <seealso cref="IObservableSettingsStore"/>
/// <seealso cref="ISettingsProvider"/>
public class SettingsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the key that was changed.
    /// </summary>
    /// <value>
    /// The unique key identifying the setting that was modified.
    /// For <see cref="SettingsChangeType.Cleared"/> events, this will be an empty string.
    /// </value>
    public string Key { get; }

    /// <summary>
    /// Gets the old value before the change, if available.
    /// </summary>
    /// <value>
    /// The value that the setting had before the change. This is populated for
    /// <see cref="SettingsChangeType.Modified"/> and <see cref="SettingsChangeType.Removed"/> events.
    /// Will be <c>null</c> for <see cref="SettingsChangeType.Added"/> and
    /// <see cref="SettingsChangeType.Cleared"/> events.
    /// </value>
    /// <remarks>
    /// The type of this value depends on how the setting was stored. It may be the original
    /// typed value or a serialized representation, depending on the provider implementation.
    /// </remarks>
    public object? OldValue { get; }

    /// <summary>
    /// Gets the new value after the change, if available.
    /// </summary>
    /// <value>
    /// The value that the setting has after the change. This is populated for
    /// <see cref="SettingsChangeType.Added"/> and <see cref="SettingsChangeType.Modified"/> events.
    /// Will be <c>null</c> for <see cref="SettingsChangeType.Removed"/> and
    /// <see cref="SettingsChangeType.Cleared"/> events.
    /// </value>
    /// <remarks>
    /// The type of this value depends on how the setting was stored. It may be the original
    /// typed value or a serialized representation, depending on the provider implementation.
    /// </remarks>
    public object? NewValue { get; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    /// <value>
    /// A <see cref="SettingsChangeType"/> value indicating whether the setting was
    /// added, modified, removed, or if all settings were cleared.
    /// </value>
    /// <seealso cref="SettingsChangeType"/>
    public SettingsChangeType ChangeType { get; }

    /// <summary>
    /// Gets the name of the provider that raised the event.
    /// </summary>
    /// <value>
    /// The unique name of the <see cref="ISettingsProvider"/> that originated this change event,
    /// or <c>null</c> if the source provider is not available or not applicable.
    /// </value>
    /// <remarks>
    /// This property is useful in composite provider scenarios to identify which
    /// underlying provider triggered the change notification.
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>
    /// Gets the timestamp when the change occurred.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing the UTC time when the change was made.
    /// This is set automatically when the event args are created.
    /// </value>
    /// <remarks>
    /// The timestamp represents when the change notification was created, which is typically
    /// immediately after the change is made. For file-based providers with file watching,
    /// there may be a slight delay between the actual file modification and the event.
    /// </remarks>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsChangedEventArgs"/> class
    /// with the specified change details.
    /// </summary>
    /// <param name="key">The key that was changed. Cannot be null (use empty string for <see cref="SettingsChangeType.Cleared"/> events).</param>
    /// <param name="changeType">The type of change that occurred.</param>
    /// <param name="oldValue">The old value before the change, if available. Pass null for added and cleared events.</param>
    /// <param name="newValue">The new value after the change, if available. Pass null for removed and cleared events.</param>
    /// <param name="providerName">The name of the provider that raised the event, or null if not applicable.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    public SettingsChangedEventArgs(
        string key,
        SettingsChangeType changeType,
        object? oldValue = null,
        object? newValue = null,
        string? providerName = null)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        ChangeType = changeType;
        OldValue = oldValue;
        NewValue = newValue;
        ProviderName = providerName;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a <see cref="SettingsChangedEventArgs"/> instance for a cleared event,
    /// where all settings were removed from the store.
    /// </summary>
    /// <param name="providerName">The name of the provider that raised the event, or null if not applicable.</param>
    /// <returns>A new <see cref="SettingsChangedEventArgs"/> instance configured for a clear operation.</returns>
    /// <remarks>
    /// This factory method provides a convenient way to create event args for clear operations,
    /// automatically setting the key to an empty string and the change type to
    /// <see cref="SettingsChangeType.Cleared"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a provider's Clear() method:
    /// _cache.Clear();
    /// OnSettingsChanged(SettingsChangedEventArgs.Cleared(Name));
    /// </code>
    /// </example>
    public static SettingsChangedEventArgs Cleared(string? providerName = null)
    {
        return new(string.Empty, SettingsChangeType.Cleared, providerName: providerName);
    }
}
