using Sharingan.Abstractions;

namespace Sharingan.Providers.Registry;

/// <summary>
/// Extension methods for <see cref="SharinganBuilder"/> to add Windows Registry provider support.
/// </summary>
/// <remarks>
/// <para>
/// The Registry provider enables storing settings in the Windows Registry, which is the
/// native configuration storage for Windows applications. It provides immediate persistence
/// without requiring explicit flush calls.
/// </para>
/// <para>
/// <strong>Platform Note:</strong> This provider is Windows-only. Using it on other platforms
/// will result in a <see cref="PlatformNotSupportedException"/>.
/// </para>
/// </remarks>
/// <seealso cref="RegistrySettingsProvider"/>
/// <seealso cref="RegistryProviderOptions"/>
public static class SharinganBuilderExtensions
{
    /// <summary>
    /// Adds a Windows Registry provider to the settings store configuration.
    /// Settings are stored immediately without requiring explicit flush.
    /// </summary>
    /// <param name="builder">The Sharingan builder instance.</param>
    /// <param name="scope">The settings scope. User scope uses HKEY_CURRENT_USER; Machine scope uses HKEY_LOCAL_MACHINE. Default is <see cref="SettingsScope.User"/>.</param>
    /// <param name="priority">The provider priority for composite stores. Default is 0.</param>
    /// <param name="configure">Optional action to configure additional Registry provider options such as custom sub-key path.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// By default, settings are stored under:
    /// <c>HKEY_CURRENT_USER\Software\{OrganizationName}\{ApplicationName}</c> for user scope, or
    /// <c>HKEY_LOCAL_MACHINE\Software\{OrganizationName}\{ApplicationName}</c> for machine scope.
    /// </para>
    /// <para>
    /// Machine scope may require administrator privileges to write.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var store = new SharinganBuilder()
    ///     .WithApplicationName("MyApp")
    ///     .WithOrganizationName("MyCompany")
    ///     .UseRegistry(SettingsScope.User)
    ///     .Build();
    /// 
    /// // Stored in HKEY_CURRENT_USER\Software\MyCompany\MyApp
    /// store.Set("theme", "dark");
    /// </code>
    /// </example>
    public static SharinganBuilder UseRegistry(
        this SharinganBuilder builder,
        SettingsScope scope = SettingsScope.User,
        int priority = 0,
        Action<RegistryProviderOptions>? configure = null)
    {
        RegistryProviderOptions options = new()
        {
            Scope = scope,
            Priority = priority
        };

        configure?.Invoke(options);

        RegistrySettingsProvider provider = new(options);
        return builder.UseProvider(provider);
    }
}
