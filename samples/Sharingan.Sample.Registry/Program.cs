using Sharingan;
using Sharingan.Abstractions;
using Sharingan.Providers.Registry;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("🔮 Sharingan - Windows Registry Provider Sample");
Console.WriteLine(new string('=', 50));

// Note: This sample only works on Windows
if (!OperatingSystem.IsWindows())
{
    Console.WriteLine("\n⚠️  This sample only runs on Windows.");
    Console.WriteLine("   The Registry provider uses Windows Registry for storage.");
    return;
}

// Example 1: Basic Registry Provider (Current User)
Console.WriteLine("\n📌 Example 1: Basic Registry Provider (HKCU)");
using RegistrySettingsProvider registryStore = new(new RegistryProviderOptions
{
    SubKeyPath = @"Software\Sharingan\SampleApp",
    Scope = SettingsScope.User, // Uses HKEY_CURRENT_USER
    CreateIfNotExists = true
});

// Set various types of values
registryStore.Set("theme", "dark");
registryStore.Set("fontSize", 14);
registryStore.Set("showSplash", true);
registryStore.Set("lastRun", DateTime.UtcNow.ToString("O"));

Console.WriteLine($"  Theme: {registryStore.Get("theme", "")}");
Console.WriteLine($"  Font Size: {registryStore.GetInt("fontSize")}");
Console.WriteLine($"  Show Splash: {registryStore.GetBool("showSplash")}");
Console.WriteLine($"  Last Run: {registryStore.Get("lastRun", "")}");

// Example 2: Using Builder Pattern
Console.WriteLine("\n📌 Example 2: Builder Pattern with Registry");
using ISettingsStore builtStore = new SharinganBuilder()
    .WithApplicationName("SampleApp")
    .WithOrganizationName("Taiizor")
    .UseRegistry(SettingsScope.User)
    .Build();

builtStore.Set("app.version", "1.0.0");
builtStore.Set("app.installPath", @"C:\Program Files\SampleApp");
Console.WriteLine($"  App Version: {builtStore.Get("app.version", "")}");
Console.WriteLine($"  Install Path: {builtStore.Get("app.installPath", "")}");

// Example 3: Change Notifications
Console.WriteLine("\n📌 Example 3: Change Notifications");
registryStore.SettingsChanged += (s, e) =>
    Console.WriteLine($"  ⚡ Setting '{e.Key}' changed ({e.ChangeType})");

registryStore.Set("user.preference", "compact");
registryStore.Set("user.language", "en-US");
registryStore.Remove("user.language");

// Example 4: Registry-Specific Benefits
Console.WriteLine("\n📌 Example 4: Registry Provider Benefits");
Console.WriteLine("  ✅ Native Windows integration");
Console.WriteLine("  ✅ Per-user (HKCU) or machine-wide (HKLM) storage");
Console.WriteLine("  ✅ Survives application reinstalls");
Console.WriteLine("  ✅ Accessible via regedit.exe");
Console.WriteLine("  ✅ Group Policy integration possible");

// Example 5: Listing All Keys
Console.WriteLine("\n📌 Example 5: All Settings");
foreach (string key in registryStore.GetAllKeys())
{
    string? value = registryStore.GetOrDefault<string>(key);
    Console.WriteLine($"  {key} = {value}");
}

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("🎉 Registry Provider sample completed successfully!");
Console.WriteLine($"  Settings saved to: HKCU\\Software\\Sharingan\\SampleApp");
