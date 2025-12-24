using Sharingan;
using Sharingan.Abstractions;
using Sharingan.Providers.Ini;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("🔮 Sharingan - INI File Provider Sample");
Console.WriteLine(new string('=', 50));

// Example 1: Basic INI Provider with section-based keys
Console.WriteLine("\n📌 Example 1: Basic INI Provider");
using IniFileSettingsProvider iniStore = new(new IniProviderOptions
{
    FilePath = "sample-config.ini",
    DefaultSection = "General",
    CreateIfNotExists = true
});

// Set values in different sections
iniStore.Set("theme", "dark"); // Goes to [General] section
iniStore.Set("Database.host", "localhost");
iniStore.Set("Database.port", 5432);
iniStore.Set("Database.username", "admin");
iniStore.Set("UI.fontSize", 14);
iniStore.Set("UI.showTooltips", true);

Console.WriteLine($"  Theme: {iniStore.Get("theme", "")}");
Console.WriteLine($"  Database Host: {iniStore.Get("Database.host", "")}");
Console.WriteLine($"  Database Port: {iniStore.GetInt("Database.port")}");
Console.WriteLine($"  Font Size: {iniStore.GetInt("UI.fontSize")}");

// Example 2: Using Builder Pattern
Console.WriteLine("\n📌 Example 2: Builder Pattern with INI File");
using ISettingsStore builtStore = new SharinganBuilder()
    .WithApplicationName("SampleApp")
    .UseIniFile("app-settings.ini", priority: 50)
    .Build();

builtStore.Set("app.version", "1.0.0");
builtStore.Set("app.debug", true);
Console.WriteLine($"  App Version: {builtStore.Get("app.version", "")}");
Console.WriteLine($"  Debug Mode: {builtStore.Get("app.debug", false)}");

// Example 3: Change Notifications
Console.WriteLine("\n📌 Example 3: Change Notifications");
iniStore.SettingsChanged += (s, e) =>
    Console.WriteLine($"  ⚡ Setting '{e.Key}' changed ({e.ChangeType})");

iniStore.Set("Notifications.enabled", true);
iniStore.Set("Notifications.sound", "default");
iniStore.Remove("Notifications.sound");

// Example 4: Listing All Keys
Console.WriteLine("\n📌 Example 4: All Settings");
foreach (string key in iniStore.GetAllKeys())
{
    string? value = iniStore.GetOrDefault<string>(key);
    Console.WriteLine($"  {key} = {value}");
}

// Save changes to file
iniStore.Flush();
builtStore.Flush();

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("🎉 INI Provider sample completed successfully!");
Console.WriteLine($"  Settings saved to: sample-config.ini");
