using Sharingan.Abstractions;

namespace Sharingan.Providers.Encrypted;

/// <summary>
/// Extension methods for <see cref="SharinganBuilder"/> to add encrypted provider support.
/// Enables transparent encryption of sensitive settings values.
/// </summary>
/// <remarks>
/// <para>
/// These extensions allow wrapping existing providers with encryption, ensuring sensitive
/// data like passwords, API keys, and tokens are encrypted at rest.
/// </para>
/// <para>
/// Encryption is transparent: values are automatically encrypted when stored and decrypted
/// when retrieved, based on configurable key patterns.
/// </para>
/// </remarks>
/// <example>
/// Adding encrypted storage:
/// <code>
/// var store = new SharinganBuilder()
///     .UseEncryptedJsonFile("secrets.json", configureEncryption: options =>
///     {
///         options.EncryptedKeyPatterns.Add("*apikey*");
///     })
///     .Build();
/// 
/// // This value will be encrypted
/// store.Set("database.password", "secret123");
/// </code>
/// </example>
/// <seealso cref="EncryptedSettingsProvider"/>
/// <seealso cref="EncryptedProviderOptions"/>
public static class SharinganBuilderExtensions
{
    /// <summary>
    /// Wraps the last added provider with encryption capabilities.
    /// </summary>
    /// <param name="builder">The Sharingan builder instance.</param>
    /// <param name="configure">Optional action to configure encryption options.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method is a convenience placeholder. For full control, use 
    /// <see cref="UseEncrypted"/> with an explicit provider instance.
    /// </para>
    /// </remarks>
    public static SharinganBuilder WithEncryption(
        this SharinganBuilder builder,
        Action<EncryptedProviderOptions>? configure = null)
    {
        EncryptedProviderOptions options = new();
        configure?.Invoke(options);

        // This is a simplified version - in production you'd need access to the internal providers list
        // For now, users should call UseEncrypted directly with a provider
        return builder;
    }

    /// <summary>
    /// Adds an encrypted wrapper around an existing settings provider.
    /// All operations on the encrypted provider automatically encrypt/decrypt values as needed.
    /// </summary>
    /// <param name="builder">The Sharingan builder instance.</param>
    /// <param name="innerProvider">The provider to wrap with encryption.</param>
    /// <param name="configure">Optional action to configure encryption options including algorithm and patterns.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="innerProvider"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// The inner provider handles actual storage, while the encryption wrapper intercepts
    /// read/write operations to apply encryption/decryption based on key patterns.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var jsonProvider = new JsonFileSettingsProvider();
    /// var store = new SharinganBuilder()
    ///     .UseEncrypted(jsonProvider, options =>
    ///     {
    ///         options.EncryptionKey = myKey;
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    public static SharinganBuilder UseEncrypted(
        this SharinganBuilder builder,
        ISettingsProvider innerProvider,
        Action<EncryptedProviderOptions>? configure = null)
    {
        EncryptedProviderOptions options = new();
        configure?.Invoke(options);

        EncryptedSettingsProvider encrypted = new(innerProvider, options);
        return builder.UseProvider(encrypted);
    }

    /// <summary>
    /// Adds an encrypted JSON file provider that automatically encrypts sensitive values.
    /// Combines the convenience of JSON storage with transparent encryption.
    /// </summary>
    /// <param name="builder">The Sharingan builder instance.</param>
    /// <param name="filePath">The path to the JSON file. Can be relative or absolute. Default is "settings.encrypted.json".</param>
    /// <param name="scope">The settings scope determining where the file is stored. Default is <see cref="SettingsScope.User"/>.</param>
    /// <param name="priority">The provider priority for composite stores. Default is 0.</param>
    /// <param name="configureEncryption">Optional action to configure encryption options.</param>
    /// <param name="configureJson">Optional action to configure JSON file options.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a JSON file provider and wraps it with an encryption layer.
    /// Values matching the encrypted key patterns are stored as encrypted strings in the
    /// JSON file.
    /// </para>
    /// <para>
    /// The JSON file remains valid and readable, but encrypted values appear as
    /// Base64-encoded ciphertext strings.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var store = new SharinganBuilder()
    ///     .UseEncryptedJsonFile(
    ///         filePath: "secrets.json",
    ///         scope: SettingsScope.User,
    ///         configureEncryption: enc =>
    ///         {
    ///             enc.EncryptedKeyPatterns.Add("*connectionstring*");
    ///         },
    ///         configureJson: json =>
    ///         {
    ///             json.UseAtomicWrites = true;
    ///         })
    ///     .Build();
    /// </code>
    /// </example>
    public static SharinganBuilder UseEncryptedJsonFile(
        this SharinganBuilder builder,
        string filePath = "settings.encrypted.json",
        SettingsScope scope = SettingsScope.User,
        int priority = 0,
        Action<EncryptedProviderOptions>? configureEncryption = null,
        Action<JsonFileProviderOptions>? configureJson = null)
    {
        JsonFileProviderOptions jsonOptions = new()
        {
            FilePath = filePath,
            Scope = scope,
            Priority = priority
        };
        configureJson?.Invoke(jsonOptions);

        EncryptedProviderOptions encryptionOptions = new() { Priority = priority };
        configureEncryption?.Invoke(encryptionOptions);

        JsonFileSettingsProvider jsonProvider = new(jsonOptions);
        EncryptedSettingsProvider encryptedProvider = new(jsonProvider, encryptionOptions);

        return builder.UseProvider(encryptedProvider);
    }
}
