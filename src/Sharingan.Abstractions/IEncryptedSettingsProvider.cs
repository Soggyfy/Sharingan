namespace Sharingan.Abstractions;

/// <summary>
/// Represents an encrypted settings provider that wraps another provider with encryption capabilities.
/// Provides additional operations for managing encryption keys and querying encryption status.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IEncryptedSettingsProvider"/> interface extends <see cref="ISettingsProvider"/>
/// to provide transparent encryption and decryption of sensitive setting values. Values matching
/// configured key patterns are automatically encrypted when stored and decrypted when retrieved.
/// </para>
/// <para>
/// Key features include:
/// <list type="bullet">
/// <item><description>Pattern-based encryption: Define which keys should be encrypted using wildcard patterns</description></item>
/// <item><description>Transparent operation: Encryption/decryption happens automatically without caller intervention</description></item>
/// <item><description>Key rotation: Support for rotating encryption keys without data loss</description></item>
/// <item><description>Query capabilities: Check if specific keys are encrypted</description></item>
/// </list>
/// </para>
/// <para>
/// The encryption provider wraps an inner provider and intercepts read/write operations to apply
/// encryption/decryption as needed. The underlying storage format is not affected by encryption.
/// </para>
/// </remarks>
/// <example>
/// Using an encrypted provider:
/// <code>
/// var settings = new SharinganBuilder()
///     .UseEncryptedJsonFile("secrets.json", configureEncryption: options =>
///     {
///         options.EncryptedKeyPatterns.Add("*password*");
///         options.EncryptedKeyPatterns.Add("*secret*");
///         options.EncryptedKeyPatterns.Add("*apikey*");
///     })
///     .Build();
/// 
/// // Values matching patterns are automatically encrypted when stored
/// settings.Set("database.password", "my-secret-password");
/// 
/// // Check encryption status
/// if (settings is IEncryptedSettingsProvider encrypted)
/// {
///     bool isEncrypted = encrypted.IsKeyEncrypted("database.password"); // true
///     
///     // Rotate encryption key (re-encrypts all values with new key)
///     await encrypted.RotateKeyAsync();
/// }
/// </code>
/// </example>
/// <seealso cref="ISettingsProvider"/>
public interface IEncryptedSettingsProvider : ISettingsProvider
{
    /// <summary>
    /// Gets a value indicating whether encryption is currently enabled for this provider.
    /// </summary>
    /// <value>
    /// <c>true</c> if the provider is actively encrypting values that match configured patterns;
    /// <c>false</c> if encryption has been disabled.
    /// </value>
    /// <remarks>
    /// When encryption is disabled, the provider behaves like a regular settings provider,
    /// storing and retrieving values without any encryption or decryption.
    /// </remarks>
    bool IsEncryptionEnabled { get; }

    /// <summary>
    /// Gets the list of key patterns that are configured for encryption.
    /// </summary>
    /// <value>
    /// A read-only list of wildcard patterns that determine which keys should have their
    /// values encrypted. Patterns support '*' (matches any sequence of characters) and
    /// '?' (matches any single character) wildcards. Pattern matching is case-insensitive.
    /// </value>
    /// <remarks>
    /// <para>
    /// Common patterns include:
    /// <list type="bullet">
    /// <item><description>"*password*" - Matches any key containing "password"</description></item>
    /// <item><description>"*secret*" - Matches any key containing "secret"</description></item>
    /// <item><description>"*key*" - Matches any key containing "key"</description></item>
    /// <item><description>"*token*" - Matches any key containing "token"</description></item>
    /// <item><description>"api.credentials.*" - Matches all keys under "api.credentials"</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// If the pattern list is empty, all values are encrypted by default.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// foreach (var pattern in encryptedProvider.EncryptedKeyPatterns)
    /// {
    ///     Console.WriteLine($"Encrypting keys matching: {pattern}");
    /// }
    /// </code>
    /// </example>
    IReadOnlyList<string> EncryptedKeyPatterns { get; }

    /// <summary>
    /// Rotates the encryption key, re-encrypting all values with a new key.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the key rotation operation.</param>
    /// <returns>A task representing the asynchronous key rotation operation.</returns>
    /// <remarks>
    /// <para>
    /// Key rotation is an important security practice that involves:
    /// <list type="number">
    /// <item><description>Generating or setting a new encryption key</description></item>
    /// <item><description>Decrypting all currently encrypted values with the old key</description></item>
    /// <item><description>Re-encrypting all values with the new key</description></item>
    /// <item><description>Persisting the changes to the underlying storage</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This operation should be performed periodically as part of security best practices,
    /// or immediately if the current key is suspected to be compromised.
    /// </para>
    /// <para>
    /// The operation is atomic - if any step fails, the original encryption state is preserved.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown when a cryptographic error occurs during key rotation.</exception>
    Task RotateKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific key's value is encrypted.
    /// </summary>
    /// <param name="key">The key to check for encryption status.</param>
    /// <returns><c>true</c> if the key's value is (or would be) encrypted based on the configured patterns; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// This method checks whether the specified key matches any of the configured
    /// <see cref="EncryptedKeyPatterns"/>. It indicates whether the value associated
    /// with this key is stored in encrypted form.
    /// </para>
    /// <para>
    /// Note that this method checks the key against patterns - it does not verify
    /// whether an actual encrypted value currently exists for the key.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Check if a key would be encrypted
    /// if (encryptedProvider.IsKeyEncrypted("database.password"))
    /// {
    ///     Console.WriteLine("This value is stored encrypted");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("This value is stored in plain text");
    /// }
    /// </code>
    /// </example>
    bool IsKeyEncrypted(string key);
}
