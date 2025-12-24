using Sharingan;
using Sharingan.Abstractions;
using Sharingan.Providers;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("🔮 Sharingan - Core Library Sample");
Console.WriteLine(new string('=', 50));

// Example 1: Quick Start with Static Settings
Console.WriteLine("\n📌 Example 1: Static Settings (Default Store)");
Settings.Default.Set("app.theme", "dark");
Settings.Default.Set("app.fontSize", 14);
Settings.Default.Set("app.language", "en-US");
Console.WriteLine($"  Theme: {Settings.Default.GetString("app.theme")}");
Console.WriteLine($"  Font Size: {Settings.Default.GetInt("app.fontSize")}");
Console.WriteLine($"  Language: {Settings.Default.GetString("app.language")}");

// Example 2: Using Builder Pattern with JSON File
Console.WriteLine("\n📌 Example 2: Builder Pattern with JSON File");
using ISettingsStore jsonStore = new SharinganBuilder()
    .WithApplicationName("SampleApp")
    .UseJsonFile("sample-settings.json", configure: opt =>
    {
        opt.UseAtomicWrites = true;
        opt.CreateIfNotExists = true;
    })
    .Build();

jsonStore.Set("database.host", "localhost");
jsonStore.Set("database.port", 5432);
jsonStore.Set("database.ssl", true);
Console.WriteLine($"  Database Host: {jsonStore.Get("database.host", "")}");
Console.WriteLine($"  Database Port: {jsonStore.Get("database.port", 0)}");
Console.WriteLine($"  SSL Enabled: {jsonStore.Get("database.ssl", false)}");

// Example 3: InMemory Provider (Session-scoped)
Console.WriteLine("\n📌 Example 3: InMemory Provider");
using ISettingsStore inMemoryStore = new SharinganBuilder()
    .WithApplicationName("SampleApp")
    .UseInMemory()
    .Build();

inMemoryStore.Set("user.name", "Test User");
inMemoryStore.Set("user.level", 42);
Console.WriteLine($"  User: {inMemoryStore.Get("user.name", "")}");
Console.WriteLine($"  Level: {inMemoryStore.Get("user.level", 0)}");

// Example 4: Change Notifications
Console.WriteLine("\n📌 Example 4: Change Notifications");
using InMemorySettingsProvider observableStore = new();
observableStore.SettingsChanged += (s, e) =>
    Console.WriteLine($"  ⚡ Setting '{e.Key}' changed ({e.ChangeType})");

observableStore.Set("dynamic.value", "hello");
observableStore.Set("dynamic.value", "world");
observableStore.Remove("dynamic.value");

// Example 5: Composite Provider (Priority-based)
Console.WriteLine("\n📌 Example 5: Composite Provider with Priorities");
using ISettingsStore compositeStore = new SharinganBuilder()
    .WithApplicationName("SampleApp")
    .UseEnvironmentVariables("SAMPLE_", priority: 100)  // Highest priority
    .UseJsonFile("defaults.json", priority: 50)         // Medium priority
    .UseInMemory(priority: 10)                          // Lowest priority (fallback)
    .Build();

compositeStore.Set("app.mode", "development");
Console.WriteLine($"  App Mode: {compositeStore.Get("app.mode", "production")}");

// Example 6: Extension Methods
Console.WriteLine("\n📌 Example 6: Extension Methods");
using InMemorySettingsProvider extStore = new();
extStore.Set("config.debug", true);
extStore.Set("config.maxRetries", 5);
extStore.Set("config.timeout", 30.5);

Console.WriteLine($"  Debug: {extStore.GetBool("config.debug")}");
Console.WriteLine($"  Max Retries: {extStore.GetInt("config.maxRetries")}");
Console.WriteLine($"  Timeout: {extStore.GetDouble("config.timeout")}s");

// GetOrSet example
string connectionString = extStore.GetOrSet("config.connectionString", "Server=localhost");
Console.WriteLine($"  Connection: {connectionString}");

// Example 7: Show Keys
Console.WriteLine("\n📌 Example 7: Show All Keys");
Console.WriteLine($"  Total settings stored: {extStore.Count}");
Console.WriteLine($"  Keys: {string.Join(", ", extStore.GetAllKeys())}");

// Example 8: Available Providers Summary
Console.WriteLine("\n📌 Example 8: Available Providers");
Console.WriteLine("  ✅ JSON File Provider (default)");
Console.WriteLine("  ✅ InMemory Provider");
Console.WriteLine("  ✅ Environment Variables Provider");
Console.WriteLine("  ✅ Composite Provider");
Console.WriteLine("  ✅ INI File Provider");
Console.WriteLine("  ✅ YAML File Provider");
Console.WriteLine("  ✅ XML File Provider");
Console.WriteLine("  ✅ TOML File Provider");
Console.WriteLine("  ✅ SQLite Provider");
Console.WriteLine("  ✅ Encrypted Provider");
Console.WriteLine("  ✅ Windows Registry Provider");

// Flush changes
jsonStore.Flush();

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("🎉 Core Library sample completed successfully!");
