using Sharingan;
using Sharingan.Abstractions;
using Sharingan.Providers.Xml;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("🔮 Sharingan - XML File Provider Sample");
Console.WriteLine(new string('=', 50));

// Example 1: Basic XML Provider
Console.WriteLine("\n📌 Example 1: Basic XML Provider");
using XmlFileSettingsProvider xmlStore = new(new XmlProviderOptions
{
    FilePath = "sample-config.xml",
    RootElementName = "configuration",
    CreateIfNotExists = true,
    UseAtomicWrites = true
});

// Set various types of values with hierarchical keys
xmlStore.Set("theme", "dark");
xmlStore.Set("database.connection.host", "localhost");
xmlStore.Set("database.connection.port", 5432);
xmlStore.Set("database.connection.timeout", 30);
xmlStore.Set("ui.window.width", 1024);
xmlStore.Set("ui.window.height", 768);

Console.WriteLine($"  Theme: {xmlStore.Get("theme", "")}");
Console.WriteLine($"  Database Host: {xmlStore.Get("database.connection.host", "")}");
Console.WriteLine($"  Database Port: {xmlStore.GetInt("database.connection.port")}");
Console.WriteLine($"  Window Size: {xmlStore.GetInt("ui.window.width")}x{xmlStore.GetInt("ui.window.height")}");

// Example 2: Using Builder Pattern
Console.WriteLine("\n📌 Example 2: Builder Pattern with XML File");
using ISettingsStore builtStore = new SharinganBuilder()
    .WithApplicationName("SampleApp")
    .UseXmlFile("app-settings.xml", priority: 50)
    .Build();

builtStore.Set("app.version", "1.0.0");
builtStore.Set("app.company", "Taiizor");
Console.WriteLine($"  App Version: {builtStore.Get("app.version", "")}");
Console.WriteLine($"  Company: {builtStore.Get("app.company", "")}");

// Example 3: Change Notifications
Console.WriteLine("\n📌 Example 3: Change Notifications");
xmlStore.SettingsChanged += (s, e) =>
    Console.WriteLine($"  ⚡ Setting '{e.Key}' changed ({e.ChangeType})");

xmlStore.Set("features.analytics", true);
xmlStore.Set("features.telemetry", false);
xmlStore.Remove("features.telemetry");

// Example 4: Listing All Keys
Console.WriteLine("\n📌 Example 4: All Settings");
foreach (string key in xmlStore.GetAllKeys())
{
    string? value = xmlStore.GetOrDefault<string>(key);
    Console.WriteLine($"  {key} = {value}");
}

// Save changes to file
xmlStore.Flush();
builtStore.Flush();

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("🎉 XML Provider sample completed successfully!");
Console.WriteLine($"  Settings saved to: sample-config.xml");
