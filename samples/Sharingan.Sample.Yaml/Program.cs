using Sharingan;
using Sharingan.Abstractions;
using Sharingan.Providers.Yaml;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("🔮 Sharingan - YAML File Provider Sample");
Console.WriteLine(new string('=', 50));

// Example 1: Basic YAML Provider
Console.WriteLine("\n📌 Example 1: Basic YAML Provider");
using YamlFileSettingsProvider yamlStore = new(new YamlProviderOptions
{
    FilePath = "sample-config.yaml",
    CreateIfNotExists = true,
    UseAtomicWrites = true
});

// Set various types of values
yamlStore.Set("theme", "dark");
yamlStore.Set("database.host", "localhost");
yamlStore.Set("database.port", 5432);
yamlStore.Set("database.ssl", true);
yamlStore.Set("ui.fontSize", 14);
yamlStore.Set("ui.language", "en-US");

Console.WriteLine($"  Theme: {yamlStore.Get("theme", "")}");
Console.WriteLine($"  Database Host: {yamlStore.Get("database.host", "")}");
Console.WriteLine($"  Database Port: {yamlStore.Get("database.port", 0)}");
Console.WriteLine($"  SSL Enabled: {yamlStore.Get("database.ssl", false)}");

// Example 2: Using Builder Pattern
Console.WriteLine("\n📌 Example 2: Builder Pattern with YAML File");
using ISettingsStore builtStore = new SharinganBuilder()
    .WithApplicationName("SampleApp")
    .UseYamlFile("app-settings.yaml", priority: 50)
    .Build();

builtStore.Set("app.version", "1.0.0");
builtStore.Set("app.environment", "development");
Console.WriteLine($"  App Version: {builtStore.Get("app.version", "")}");
Console.WriteLine($"  Environment: {builtStore.Get("app.environment", "")}");

// Example 3: Change Notifications
Console.WriteLine("\n📌 Example 3: Change Notifications");
yamlStore.SettingsChanged += (s, e) =>
    Console.WriteLine($"  ⚡ Setting '{e.Key}' changed ({e.ChangeType})");

yamlStore.Set("logging.level", "Debug");
yamlStore.Set("logging.format", "json");
yamlStore.Remove("logging.format");

// Example 4: Listing All Keys
Console.WriteLine("\n📌 Example 4: All Settings");
foreach (string key in yamlStore.GetAllKeys())
{
    string? value = yamlStore.GetOrDefault<string>(key);
    Console.WriteLine($"  {key} = {value}");
}

// Save changes to file
yamlStore.Flush();
builtStore.Flush();

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("🎉 YAML Provider sample completed successfully!");
Console.WriteLine($"  Settings saved to: sample-config.yaml");
