using Sharingan;
using Sharingan.Abstractions;
using Sharingan.Providers.Toml;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("🔮 Sharingan - TOML File Provider Sample");
Console.WriteLine(new string('=', 50));

// Example 1: Basic TOML Provider
Console.WriteLine("\n📌 Example 1: Basic TOML Provider");
using TomlFileSettingsProvider tomlStore = new(new TomlProviderOptions
{
    FilePath = "sample-config.toml",
    CreateIfNotExists = true,
    UseAtomicWrites = true
});

// Set various types of values - TOML supports typed values natively
tomlStore.Set("title", "Sample Application");
tomlStore.Set("database.host", "localhost");
tomlStore.Set("database.port", 5432);
tomlStore.Set("database.enabled", true);
tomlStore.Set("server.timeout", 30.5);
tomlStore.Set("owner.name", "John Doe");

Console.WriteLine($"  Title: {tomlStore.Get("title", "")}");
Console.WriteLine($"  Database Host: {tomlStore.Get("database.host", "")}");
Console.WriteLine($"  Database Port: {tomlStore.Get("database.port", 0)}");
Console.WriteLine($"  Enabled: {tomlStore.Get("database.enabled", false)}");
Console.WriteLine($"  Server Timeout: {tomlStore.Get("server.timeout", 0.0)}s");

// Example 2: Using Builder Pattern
Console.WriteLine("\n📌 Example 2: Builder Pattern with TOML File");
using ISettingsStore builtStore = new SharinganBuilder()
    .WithApplicationName("SampleApp")
    .UseTomlFile("app-settings.toml", priority: 50)
    .Build();

builtStore.Set("app.version", "1.0.0");
builtStore.Set("app.maxRetries", 3);
Console.WriteLine($"  App Version: {builtStore.Get("app.version", "")}");
Console.WriteLine($"  Max Retries: {builtStore.Get("app.maxRetries", 0)}");

// Example 3: Change Notifications
Console.WriteLine("\n📌 Example 3: Change Notifications");
tomlStore.SettingsChanged += (s, e) =>
    Console.WriteLine($"  ⚡ Setting '{e.Key}' changed ({e.ChangeType})");

tomlStore.Set("cache.enabled", true);
tomlStore.Set("cache.ttl", 3600);
tomlStore.Remove("cache.ttl");

// Example 4: Listing All Keys
Console.WriteLine("\n📌 Example 4: All Settings");
foreach (string key in tomlStore.GetAllKeys())
{
    string? value = tomlStore.GetOrDefault<string>(key);
    Console.WriteLine($"  {key} = {value}");
}

// Save changes to file
tomlStore.Flush();
builtStore.Flush();

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("🎉 TOML Provider sample completed successfully!");
Console.WriteLine($"  Settings saved to: sample-config.toml");
