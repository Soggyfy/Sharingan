namespace Sharingan.Abstractions;

/// <summary>
/// Represents a settings store that supports change notifications through events.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IObservableSettingsStore"/> interface extends <see cref="ISettingsStore"/>
/// to provide change notification capabilities. This enables reactive programming patterns
/// where consumers can subscribe to setting changes and respond accordingly.
/// </para>
/// <para>
/// This interface is particularly useful for:
/// <list type="bullet">
/// <item><description>UI data binding scenarios where controls need to update when settings change</description></item>
/// <item><description>Multi-process applications where one process needs to react to changes made by another</description></item>
/// <item><description>Configuration hot-reload scenarios where the application adapts to setting changes without restart</description></item>
/// <item><description>Audit logging of configuration changes</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Subscribing to setting changes:
/// <code>
/// if (store is IObservableSettingsStore observable)
/// {
///     observable.SettingsChanged += (sender, args) =>
///     {
///         Console.WriteLine($"Setting '{args.Key}' changed from '{args.OldValue}' to '{args.NewValue}'");
///         
///         if (args.ChangeType == SettingsChangeType.Cleared)
///         {
///             Console.WriteLine("All settings were cleared!");
///         }
///     };
/// }
/// </code>
/// </example>
/// <seealso cref="ISettingsStore"/>
/// <seealso cref="SettingsChangedEventArgs"/>
/// <seealso cref="SettingsChangeType"/>
public interface IObservableSettingsStore : ISettingsStore
{
    /// <summary>
    /// Occurs when a setting value changes in the store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised whenever a setting is added, modified, removed, or when all settings
    /// are cleared. The <see cref="SettingsChangedEventArgs"/> provides detailed information about
    /// the change, including:
    /// <list type="bullet">
    /// <item><description><see cref="SettingsChangedEventArgs.Key"/>: The key that was affected</description></item>
    /// <item><description><see cref="SettingsChangedEventArgs.ChangeType"/>: Whether the setting was added, modified, removed, or cleared</description></item>
    /// <item><description><see cref="SettingsChangedEventArgs.OldValue"/>: The previous value (for modifications and removals)</description></item>
    /// <item><description><see cref="SettingsChangedEventArgs.NewValue"/>: The new value (for additions and modifications)</description></item>
    /// <item><description><see cref="SettingsChangedEventArgs.ProviderName"/>: The name of the provider that raised the event</description></item>
    /// <item><description><see cref="SettingsChangedEventArgs.Timestamp"/>: When the change occurred</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Event handlers should be lightweight and should not perform long-running or blocking operations,
    /// as they may be invoked synchronously from the thread that made the change.
    /// </para>
    /// <para>
    /// For file-based stores with file watching enabled, this event may also be raised when
    /// external changes to the settings file are detected.
    /// </para>
    /// </remarks>
    /// <seealso cref="SettingsChangedEventArgs"/>
    /// <seealso cref="SettingsChangeType"/>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
}
