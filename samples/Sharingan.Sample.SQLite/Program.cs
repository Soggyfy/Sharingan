using Sharingan;
using Sharingan.Abstractions;
using Sharingan.Providers.SQLite;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("🔮 Sharingan - SQLite Provider Sample");
Console.WriteLine(new string('=', 50));

// Example 1: Basic SQLite Provider
Console.WriteLine("\n📌 Example 1: Basic SQLite Provider");
using SQLiteSettingsProvider sqliteStore = new(new SQLiteProviderOptions
{
    DatabasePath = "sample-settings.db",
    TableName = "Settings",
    CreateIfNotExists = true
});

// Set various types of values
sqliteStore.Set("theme", "dark");
sqliteStore.Set("database.connectionString", "Server=localhost;Database=MyApp");
sqliteStore.Set("database.maxConnections", 100);
sqliteStore.Set("cache.enabled", true);
sqliteStore.Set("cache.ttlSeconds", 3600);
sqliteStore.Set("logging.level", "Information");

Console.WriteLine($"  Theme: {sqliteStore.Get("theme", "")}");
Console.WriteLine($"  Connection String: {sqliteStore.Get("database.connectionString", "")}");
Console.WriteLine($"  Max Connections: {sqliteStore.GetInt("database.maxConnections")}");
Console.WriteLine($"  Cache Enabled: {sqliteStore.GetBool("cache.enabled")}");
Console.WriteLine($"  Cache TTL: {sqliteStore.GetInt("cache.ttlSeconds")}s");

// Example 2: Using Builder Pattern
Console.WriteLine("\n📌 Example 2: Builder Pattern with SQLite");
using ISettingsStore builtStore = new SharinganBuilder()
    .WithApplicationName("SampleApp")
    .UseSQLite("app-settings.db", priority: 50)
    .Build();

builtStore.Set("app.version", "1.0.0");
builtStore.Set("app.installDate", DateTime.UtcNow.ToString("O"));
Console.WriteLine($"  App Version: {builtStore.Get("app.version", "")}");
Console.WriteLine($"  Install Date: {builtStore.Get("app.installDate", "")}");

// Example 3: Change Notifications
Console.WriteLine("\n📌 Example 3: Change Notifications");
sqliteStore.SettingsChanged += (s, e) =>
    Console.WriteLine($"  ⚡ Setting '{e.Key}' changed ({e.ChangeType})");

sqliteStore.Set("user.lastLogin", DateTime.UtcNow.ToString("O"));
sqliteStore.Set("user.sessionCount", 42);
sqliteStore.Remove("user.sessionCount");

// Example 4: ACID Properties Demo
Console.WriteLine("\n📌 Example 4: SQLite ACID Properties");
Console.WriteLine("  ✅ Atomicity: All changes are atomic transactions");
Console.WriteLine("  ✅ Consistency: Database integrity maintained");
Console.WriteLine("  ✅ Isolation: Concurrent access supported");
Console.WriteLine("  ✅ Durability: Changes persist across crashes");

// Example 5: Listing All Keys
Console.WriteLine("\n📌 Example 5: All Settings");
foreach (string key in sqliteStore.GetAllKeys())
{
    string? value = sqliteStore.GetOrDefault<string>(key);
    Console.WriteLine($"  {key} = {value}");
}

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("🎉 SQLite Provider sample completed successfully!");
Console.WriteLine($"  Settings saved to: sample-settings.db");
