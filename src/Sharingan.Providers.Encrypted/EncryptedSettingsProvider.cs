using Sharingan.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Sharingan.Providers.Encrypted;

/// <summary>
/// An encryption wrapper provider that transparently encrypts and decrypts setting values
/// based on configurable key patterns. Uses AES-256-CBC for maximum cross-framework compatibility.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EncryptedSettingsProvider"/> wraps an inner provider and intercepts read/write
/// operations to apply encryption and decryption. Values are encrypted before being passed to the
/// inner provider for storage, and decrypted when retrieved.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item><description>Pattern-based encryption: Only keys matching configured patterns are encrypted</description></item>
/// <item><description>AES-256-CBC: Industry-standard encryption with 256-bit key strength</description></item>
/// <item><description>Automatic IV: Each encryption uses a random initialization vector</description></item>
/// <item><description>Transparent operation: Calling code doesn't need to handle encryption</description></item>
/// <item><description>Key rotation support: Re-encrypt all values with a new key</description></item>
/// </list>
/// </para>
/// <para>
/// Security notes:
/// <list type="bullet">
/// <item><description>If no key is provided, one is derived from machine/user info (convenient but less portable)</description></item>
/// <item><description>Encrypted values are stored as Base64-encoded strings containing IV + ciphertext</description></item>
/// <item><description>Keys not matching any pattern are stored in plain text for performance</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Using encrypted provider:
/// <code>
/// var jsonProvider = new JsonFileSettingsProvider();
/// var encryptedProvider = new EncryptedSettingsProvider(jsonProvider, new EncryptedProviderOptions
/// {
///     EncryptedKeyPatterns = ["*password*", "*secret*", "*token*"]
/// });
/// 
/// // This value will be encrypted before storage
/// encryptedProvider.Set("database.password", "my-secret-password");
/// 
/// // Retrieved value is automatically decrypted
/// var password = encryptedProvider.Get("database.password", "");
/// </code>
/// </example>
/// <seealso cref="IEncryptedSettingsProvider"/>
/// <seealso cref="EncryptedProviderOptions"/>
public class EncryptedSettingsProvider : IEncryptedSettingsProvider
{
    /// <summary>
    /// The inner provider that handles actual storage of (encrypted) values.
    /// </summary>
    private readonly ISettingsProvider _innerProvider;

    /// <summary>
    /// Configuration options for encryption behavior.
    /// </summary>
    private readonly EncryptedProviderOptions _options;

    /// <summary>
    /// The 256-bit encryption key.
    /// </summary>
    private readonly byte[] _key;

    /// <summary>
    /// Compiled regex patterns for matching keys that should be encrypted.
    /// </summary>
    private readonly Regex[] _patterns;

    /// <summary>
    /// Tracks whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <inheritdoc />
    /// <value>Returns "Encrypted:{innerProviderName}" for identification.</value>
    public string Name => $"Encrypted:{_innerProvider.Name}";

    /// <inheritdoc />
    /// <value>Returns the read-only status of the inner provider.</value>
    public bool IsReadOnly => _innerProvider.IsReadOnly;

    /// <inheritdoc />
    public int Priority => _options.Priority;

    /// <inheritdoc />
    /// <value>Returns the scope of the inner provider.</value>
    public SettingsScope Scope => _innerProvider.Scope;

    /// <inheritdoc />
    public int Count => _innerProvider.Count;

    /// <inheritdoc />
    /// <value>Always returns <c>true</c> since this is an encryption provider.</value>
    public bool IsEncryptionEnabled => true;

    /// <inheritdoc />
    public IReadOnlyList<string> EncryptedKeyPatterns => _options.EncryptedKeyPatterns;

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedSettingsProvider"/> class
    /// wrapping the specified inner provider with encryption capabilities.
    /// </summary>
    /// <param name="innerProvider">The provider to wrap. This provider handles actual storage. Cannot be null.</param>
    /// <param name="options">Optional encryption configuration. If null, default options are used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="innerProvider"/> is null.</exception>
    /// <remarks>
    /// If <see cref="EncryptedProviderOptions.EncryptionKey"/> is null, a key is derived from
    /// the machine name and current username using SHA-256. This provides convenience but
    /// means encrypted data can only be decrypted on the same machine by the same user.
    /// </remarks>
    public EncryptedSettingsProvider(ISettingsProvider innerProvider, EncryptedProviderOptions? options = null)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _options = options ?? new EncryptedProviderOptions();
        _key = _options.EncryptionKey ?? DeriveDefaultKey();
        _patterns = [.. _options.EncryptedKeyPatterns.Select(p => new Regex(WildcardToRegex(p), RegexOptions.IgnoreCase | RegexOptions.Compiled))];

        _innerProvider.SettingsChanged += (s, e) => SettingsChanged?.Invoke(this, e);
    }

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        string? encrypted = _innerProvider.Get<string>(key, null!);
        if (encrypted is null)
        {
            return defaultValue;
        }

        if (!ShouldEncrypt(key))
        {
            return _innerProvider.Get(key, defaultValue);
        }

        try
        {
            string decrypted = Decrypt(encrypted);
            if (typeof(T) == typeof(string))
            {
                return (T)(object)decrypted;
            }

            return JsonSerializer.Deserialize<T>(decrypted) ?? defaultValue;
        }
        catch { return defaultValue; }
    }

    /// <inheritdoc />
    public T? GetOrDefault<T>(string key)
    {
        return Get<T>(key, default!);
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T? value)
    {
        try
        {
            value = Get<T>(key, default!);
            return value is not null;
        }
        catch { value = default; return false; }
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
        if (!ShouldEncrypt(key))
        {
            _innerProvider.Set(key, value);
            return;
        }

        string plaintext = value is string s ? s : JsonSerializer.Serialize(value);
        string encrypted = Encrypt(plaintext);
        _innerProvider.Set(key, encrypted);
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        return _innerProvider.Remove(key);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _innerProvider.Clear();
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        return _innerProvider.ContainsKey(key);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllKeys()
    {
        return _innerProvider.GetAllKeys();
    }

    /// <inheritdoc />
    public void Flush()
    {
        _innerProvider.Flush();
    }

    /// <inheritdoc />
    public void Reload()
    {
        _innerProvider.Reload();
    }

    /// <inheritdoc />
    public bool IsKeyEncrypted(string key)
    {
        return ShouldEncrypt(key);
    }

    /// <inheritdoc />
    public async Task RotateKeyAsync(CancellationToken cancellationToken = default)
    {
        List<string> keys = GetAllKeys().ToList();
        foreach (string? key in keys)
        {
            if (ShouldEncrypt(key))
            {
                string? decrypted = Get<string>(key, null!);
                if (decrypted is not null)
                {
                    Set(key, decrypted);
                }
            }
        }
        await _innerProvider.FlushAsync(cancellationToken).ConfigureAwait(false);
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
        return _innerProvider.FlushAsync(ct);
    }

    public Task ReloadAsync(CancellationToken ct = default) { Reload(); return Task.CompletedTask; }

    private bool ShouldEncrypt(string key)
    {
        if (_patterns.Length == 0)
        {
            return true;
        }

        return _patterns.Any(p => p.IsMatch(key));
    }

    // AES-256-CBC encryption (cross-platform compatible)
    private string Encrypt(string plaintext)
    {
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] ciphertext = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // Combine IV + ciphertext
        byte[] result = new byte[aes.IV.Length + ciphertext.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    private string Decrypt(string encrypted)
    {
        byte[] data = Convert.FromBase64String(encrypted);

        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Extract IV (first 16 bytes)
        byte[] iv = new byte[16];
        byte[] ciphertext = new byte[data.Length - 16];
        Buffer.BlockCopy(data, 0, iv, 0, 16);
        Buffer.BlockCopy(data, 16, ciphertext, 0, ciphertext.Length);

        aes.IV = iv;

        using ICryptoTransform decryptor = aes.CreateDecryptor();
        byte[] plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] DeriveDefaultKey()
    {
        string machineId = Environment.MachineName + Environment.UserName;

#if NET7_0_OR_GREATER
        return SHA256.HashData(Encoding.UTF8.GetBytes(machineId));
#else
        using SHA256 sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(machineId));
#endif
    }

    private static string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _innerProvider.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
