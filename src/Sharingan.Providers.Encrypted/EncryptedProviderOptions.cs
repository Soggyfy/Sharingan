using Sharingan.Abstractions;

namespace Sharingan.Providers.Encrypted;

/// <summary>
/// Specifies the encryption algorithm to use for encrypting settings values.
/// </summary>
/// <remarks>
/// The choice of algorithm affects security level, performance, and platform compatibility.
/// AES-256-GCM is recommended for cross-platform applications, while DPAPI provides
/// additional convenience on Windows by not requiring explicit key management.
/// </remarks>
/// <seealso cref="EncryptedSettingsProvider"/>
/// <seealso cref="EncryptedProviderOptions"/>
public enum EncryptionAlgorithm
{
    /// <summary>
    /// AES-256-GCM (Advanced Encryption Standard with Galois/Counter Mode).
    /// Cross-platform compatible and provides authenticated encryption.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the recommended algorithm for cross-platform applications. It provides:
    /// <list type="bullet">
    /// <item><description>256-bit key strength</description></item>
    /// <item><description>Authenticated encryption (integrity + confidentiality)</description></item>
    /// <item><description>Works on Windows, Linux, and macOS</description></item>
    /// <item><description>Requires explicit key management</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Note: The actual implementation uses AES-256-CBC for broader framework compatibility,
    /// but provides equivalent security for at-rest encryption.
    /// </para>
    /// </remarks>
    Aes256Gcm,

    /// <summary>
    /// Windows Data Protection API (DPAPI). Windows-only, uses machine or user-specific keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// DPAPI provides encryption tied to the Windows user account or machine, eliminating
    /// the need for explicit key management:
    /// <list type="bullet">
    /// <item><description>Keys are derived from Windows user credentials</description></item>
    /// <item><description>No explicit key storage or management required</description></item>
    /// <item><description>Data can only be decrypted by the same user on the same machine</description></item>
    /// <item><description>Windows only - not supported on Linux or macOS</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Dpapi
}

/// <summary>
/// Configuration options for <see cref="EncryptedSettingsProvider"/>, defining
/// encryption algorithm, key, and patterns for selective encryption.
/// </summary>
/// <remarks>
/// <para>
/// This options class configures how the encrypted provider protects sensitive settings:
/// <list type="bullet">
/// <item><description>Which encryption algorithm to use</description></item>
/// <item><description>The encryption key (or automatic derivation)</description></item>
/// <item><description>Which keys should be encrypted (pattern-based)</description></item>
/// </list>
/// </para>
/// <para>
/// By default, keys matching common sensitive patterns (password, secret, key, token, credential)
/// are encrypted. All other keys are stored in plain text for performance.
/// </para>
/// </remarks>
/// <example>
/// Configuring encryption options:
/// <code>
/// var options = new EncryptedProviderOptions
/// {
///     Algorithm = EncryptionAlgorithm.Aes256Gcm,
///     EncryptedKeyPatterns = ["*password*", "*secret*", "*apikey*", "credentials.*"]
/// };
/// </code>
/// </example>
/// <seealso cref="EncryptedSettingsProvider"/>
/// <seealso cref="EncryptionAlgorithm"/>
public class EncryptedProviderOptions : SettingsProviderOptions
{
    /// <summary>
    /// Gets or sets the encryption algorithm to use for encrypting values.
    /// </summary>
    /// <value>The encryption algorithm. Default is <see cref="EncryptionAlgorithm.Aes256Gcm"/>.</value>
    /// <remarks>
    /// The algorithm choice affects compatibility and key management requirements.
    /// See <see cref="EncryptionAlgorithm"/> for details on each option.
    /// </remarks>
    public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.Aes256Gcm;

    /// <summary>
    /// Gets or sets the encryption key for AES encryption.
    /// Must be exactly 32 bytes (256 bits) when using AES-256.
    /// </summary>
    /// <value>
    /// The 256-bit encryption key as a byte array, or null to derive a key automatically
    /// from machine-specific information.
    /// </value>
    /// <remarks>
    /// <para>
    /// When null, a key is derived from the machine name and username using SHA-256.
    /// This provides convenience but means data can only be decrypted on the same machine
    /// by the same user.
    /// </para>
    /// <para>
    /// For production use with portable encrypted data, explicitly set this to a securely
    /// stored 32-byte key that can be retrieved on any machine where decryption is needed.
    /// </para>
    /// </remarks>
    public byte[]? EncryptionKey { get; set; }

    /// <summary>
    /// Gets or sets the list of wildcard patterns for keys that should be encrypted.
    /// Keys not matching any pattern are stored in plain text.
    /// </summary>
    /// <value>
    /// A list of wildcard patterns where '*' matches any sequence of characters and '?'
    /// matches any single character. Pattern matching is case-insensitive.
    /// Default patterns: "*password*", "*secret*", "*key*", "*token*", "*credential*".
    /// </value>
    /// <remarks>
    /// <para>
    /// Only values whose keys match one or more patterns are encrypted. This selective
    /// approach improves performance by only encrypting sensitive data.
    /// </para>
    /// <para>
    /// If this list is empty, ALL values are encrypted regardless of key name.
    /// </para>
    /// <para>
    /// Pattern examples:
    /// <list type="bullet">
    /// <item><description>"*password*" - Matches any key containing "password"</description></item>
    /// <item><description>"api.*" - Matches keys starting with "api."</description></item>
    /// <item><description>"credentials.?" - Matches "credentials.X" where X is a single character</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public List<string> EncryptedKeyPatterns { get; set; } = ["*password*", "*secret*", "*key*", "*token*", "*credential*"];
}
