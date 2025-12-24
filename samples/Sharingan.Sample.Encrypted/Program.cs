using Sharingan;
using Sharingan.Abstractions;
using Sharingan.Providers;
using Sharingan.Providers.Encrypted;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("🔮 Sharingan - Encrypted Provider Sample");
Console.WriteLine(new string('=', 50));

// Example 1: Basic Encrypted Provider with pattern matching
Console.WriteLine("\n📌 Example 1: Encrypted Provider with Pattern Matching");

// Create an inner provider (InMemory for demo, but can be any provider)
using InMemorySettingsProvider innerProvider = new();

// Wrap with encryption - only keys matching patterns are encrypted
using EncryptedSettingsProvider encryptedStore = new(innerProvider, new EncryptedProviderOptions
{
    EncryptedKeyPatterns = ["*password*", "*secret*", "*token*", "*key*"]
});

// Set sensitive values (will be encrypted)
encryptedStore.Set("database.password", "MySecr3tP@ssw0rd!");
encryptedStore.Set("api.secret", "sk_live_abc123xyz789");
encryptedStore.Set("auth.token", "eyJhbGciOiJIUzI1NiIs...");

// Set non-sensitive values (will NOT be encrypted)
encryptedStore.Set("app.theme", "dark");
encryptedStore.Set("app.version", "1.0.0");

Console.WriteLine($"  Database Password: {encryptedStore.Get("database.password", "")}");
Console.WriteLine($"  API Secret: {encryptedStore.Get("api.secret", "")}");
Console.WriteLine($"  Auth Token: {encryptedStore.Get("auth.token", "")}");
Console.WriteLine($"  Theme: {encryptedStore.Get("app.theme", "")}");

// Example 2: Check which keys are encrypted
Console.WriteLine("\n📌 Example 2: Encryption Status Check");
string[] keysToCheck = new[] { "database.password", "api.secret", "app.theme", "app.version" };
foreach (string? key in keysToCheck)
{
    bool isEncrypted = encryptedStore.IsKeyEncrypted(key);
    string status = isEncrypted ? "🔒 Encrypted" : "📄 Plain text";
    Console.WriteLine($"  {key}: {status}");
}

// Example 3: Using Builder Pattern with UseEncryptedJsonFile
Console.WriteLine("\n📌 Example 3: Builder Pattern with Encrypted JSON File");
using ISettingsStore builtStore = new SharinganBuilder()
    .WithApplicationName("SecureApp")
    .UseEncryptedJsonFile(
        "encrypted-settings.json",
        configureEncryption: options => options.EncryptedKeyPatterns = ["*credential*", "*password*"])
    .Build();

builtStore.Set("user.credential", "admin:password123");
builtStore.Set("user.name", "John Doe");

Console.WriteLine($"  User Credential: {builtStore.Get("user.credential", "")}");
Console.WriteLine($"  User Name: {builtStore.Get("user.name", "")}");

// Example 4: View raw encrypted values in inner provider
Console.WriteLine("\n📌 Example 4: Raw Values in Inner Provider");
Console.WriteLine("  (Encrypted values are stored as Base64-encoded ciphertext)");
foreach (string key in innerProvider.GetAllKeys())
{
    string rawValue = innerProvider.Get(key, "");
    string displayValue = rawValue.Length > 50 ? rawValue[..50] + "..." : rawValue;
    Console.WriteLine($"  {key} = {displayValue}");
}

// Example 5: Security Features
Console.WriteLine("\n📌 Example 5: Security Features");
Console.WriteLine("  ✅ AES-256-CBC encryption");
Console.WriteLine("  ✅ Random IV per encryption");
Console.WriteLine("  ✅ Pattern-based selective encryption");
Console.WriteLine("  ✅ Automatic key derivation (machine-specific)");
Console.WriteLine("  ✅ Key rotation support");

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("🎉 Encrypted Provider sample completed successfully!");
