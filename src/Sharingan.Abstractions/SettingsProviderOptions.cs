namespace Sharingan.Abstractions;

/// <summary>
/// Configuration options for settings providers, defining common properties that control
/// provider behavior including priority, scope, and serialization settings.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SettingsProviderOptions"/> serves as the base configuration class for all settings
/// providers in the Sharingan library. It defines common properties that are applicable to most
/// provider implementations, such as priority ordering, read-only mode, and scope configuration.
/// </para>
/// <para>
/// Specific provider implementations (such as <c>JsonFileProviderOptions</c>, <c>IniProviderOptions</c>,
/// etc.) extend this class to add provider-specific configuration options while inheriting these
/// common settings.
/// </para>
/// <para>
/// When using the builder pattern, these options are typically configured through
/// fluent API methods or passed directly to provider factory methods.
/// </para>
/// </remarks>
/// <example>
/// Using provider options:
/// <code>
/// var options = new SettingsProviderOptions
/// {
///     Priority = 50,
///     Scope = SettingsScope.User,
///     ApplicationName = "MyApp",
///     OrganizationName = "MyCompany"
/// };
/// </code>
/// </example>
/// <seealso cref="SettingsScope"/>
/// <seealso cref="ISettingsProvider"/>
/// <seealso cref="ISettingsSerializer"/>
public class SettingsProviderOptions
{
    /// <summary>
    /// Gets or sets the priority of this provider for composite/chained scenarios.
    /// </summary>
    /// <value>
    /// An integer representing the provider's priority. Higher values indicate higher priority
    /// (checked first when reading values). The default value is 0.
    /// </value>
    /// <remarks>
    /// <para>
    /// When multiple providers are combined in a composite store, providers are consulted in
    /// descending priority order when reading a value. The first provider that contains the
    /// requested key returns the value.
    /// </para>
    /// <para>
    /// Common priority conventions:
    /// <list type="bullet">
    /// <item><description>100+: Environment variables and command-line overrides (highest priority)</description></item>
    /// <item><description>50-99: User-specific settings</description></item>
    /// <item><description>0-49: Application defaults and fallback values (lowest priority)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this provider is read-only.
    /// </summary>
    /// <value>
    /// <c>true</c> if the provider should reject all write operations (Set, Remove, Clear);
    /// <c>false</c> if the provider supports both read and write operations.
    /// The default value is <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When a provider is configured as read-only, any attempt to call write operations
    /// (Set, Remove, Clear) will throw a <see cref="NotSupportedException"/>.
    /// </para>
    /// <para>
    /// Some providers are inherently read-only (such as environment variables) and will
    /// override this setting. For other providers, this option allows you to protect
    /// settings from accidental modification.
    /// </para>
    /// </remarks>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the settings scope for this provider, determining the storage location
    /// and accessibility of settings.
    /// </summary>
    /// <value>
    /// A <see cref="SettingsScope"/> value indicating where settings should be stored.
    /// The default value is <see cref="SettingsScope.User"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// The scope affects the physical storage location of settings files:
    /// <list type="bullet">
    /// <item><description><see cref="SettingsScope.User"/>: Per-user directory (e.g., %APPDATA% on Windows)</description></item>
    /// <item><description><see cref="SettingsScope.Machine"/>: Machine-wide directory (e.g., %PROGRAMDATA% on Windows)</description></item>
    /// <item><description><see cref="SettingsScope.Application"/>: Next to the application executable</description></item>
    /// <item><description><see cref="SettingsScope.Session"/>: Temporary/volatile storage</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <seealso cref="SettingsScope"/>
    public SettingsScope Scope { get; set; } = SettingsScope.User;

    /// <summary>
    /// Gets or sets the application name used for path resolution when storing settings files.
    /// </summary>
    /// <value>
    /// The application name to use when constructing the settings storage path.
    /// If <c>null</c>, the entry assembly name is used as a fallback, or "Sharingan" as a last resort.
    /// </value>
    /// <remarks>
    /// <para>
    /// The application name is combined with the scope to determine the full path for settings storage.
    /// For example, with <see cref="SettingsScope.User"/> scope on Windows, settings might be stored at:
    /// <c>%APPDATA%\{OrganizationName}\{ApplicationName}\settings.json</c>
    /// </para>
    /// <para>
    /// It is recommended to explicitly set this value to ensure consistent storage locations
    /// across different deployment scenarios.
    /// </para>
    /// </remarks>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the organization name used for path resolution when storing settings files.
    /// </summary>
    /// <value>
    /// The organization name to use when constructing the settings storage path.
    /// If <c>null</c>, the organization name is omitted from the path.
    /// </value>
    /// <remarks>
    /// <para>
    /// When specified, the organization name creates an additional directory level in the settings path.
    /// This is useful for organizing multiple applications from the same organization under a common folder.
    /// </para>
    /// <para>
    /// Example paths:
    /// <list type="bullet">
    /// <item><description>With organization: <c>%APPDATA%\MyCompany\MyApp\settings.json</c></description></item>
    /// <item><description>Without organization: <c>%APPDATA%\MyApp\settings.json</c></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? OrganizationName { get; set; }

    /// <summary>
    /// Gets or sets a custom serializer for this provider to use when converting values
    /// to and from their string representation.
    /// </summary>
    /// <value>
    /// An <see cref="ISettingsSerializer"/> implementation to use for serialization.
    /// If <c>null</c>, the default JSON serializer will be used.
    /// </value>
    /// <remarks>
    /// <para>
    /// Custom serializers can be provided to:
    /// <list type="bullet">
    /// <item><description>Use alternative serialization formats (XML, YAML, MessagePack, etc.)</description></item>
    /// <item><description>Apply custom serialization rules or transformations</description></item>
    /// <item><description>Handle special types that require custom conversion logic</description></item>
    /// <item><description>Apply compression or other processing to serialized data</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Note that the serializer specified here takes precedence over any serializer set at the
    /// builder level using the builder's WithSerializer method.
    /// </para>
    /// </remarks>
    /// <seealso cref="ISettingsSerializer"/>
    public ISettingsSerializer? Serializer { get; set; }
}
