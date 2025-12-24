using Microsoft.Extensions.Configuration;
using Sharingan;
using Sharingan.Abstractions;
using Sharingan.Extensions.Configuration;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("🔮 Sharingan - Configuration Integration Sample");
Console.WriteLine(new string('=', 50));

// Example 1: Basic Configuration Builder Integration
Console.WriteLine("\n📌 Example 1: Configuration Builder Integration");

// First, set up some settings using Sharingan directly
using ISettingsStore settingsStore = new SharinganBuilder()
    .WithApplicationName("ConfigApp")
    .UseJsonFile("config-settings.json")
    .Build();

settingsStore.Set("Database.Host", "localhost");
settingsStore.Set("Database.Port", "5432");
settingsStore.Set("Database.Name", "myapp");
settingsStore.Set("Logging.Level", "Information");
settingsStore.Set("App.Version", "1.0.0");
settingsStore.Flush();

// Now, use Sharingan as a configuration source
IConfigurationRoot configuration = new ConfigurationBuilder()
    .AddSharingan(builder => builder.WithApplicationName("ConfigApp")
               .UseJsonFile("config-settings.json"))
    .Build();

// Access settings via IConfiguration (note: keys use colons instead of dots)
Console.WriteLine($"  Database Host: {configuration["Database:Host"]}");
Console.WriteLine($"  Database Port: {configuration["Database:Port"]}");
Console.WriteLine($"  Logging Level: {configuration["Logging:Level"]}");
Console.WriteLine($"  App Version: {configuration["App:Version"]}");

// Example 2: Configuration with multiple sources
Console.WriteLine("\n📌 Example 2: Multi-Source Configuration");

// Set environment variable for demo
Environment.SetEnvironmentVariable("MYAPP_Database__Host", "production-db.example.com");

IConfigurationRoot multiConfig = new ConfigurationBuilder()
    .AddSharingan(builder => builder.WithApplicationName("ConfigApp")
               .UseJsonFile("config-settings.json"))
    .AddEnvironmentVariables("MYAPP_") // Override with env vars
    .Build();

Console.WriteLine($"  Database Host (with env override): {multiConfig["Database:Host"]}");

// Example 3: GetSection and Binding
Console.WriteLine("\n📌 Example 3: Configuration Sections");
IConfigurationSection dbSection = configuration.GetSection("Database");
Console.WriteLine($"  Database Section exists: {dbSection.Exists()}");
Console.WriteLine($"  Database children:");
foreach (IConfigurationSection child in dbSection.GetChildren())
{
    Console.WriteLine($"    {child.Key} = {child.Value}");
}

// Example 4: Strongly-Typed Configuration
Console.WriteLine("\n📌 Example 4: Strongly-Typed Configuration");
DatabaseConfig dbConfig = new();
configuration.GetSection("Database").Bind(dbConfig);
Console.WriteLine($"  Bound config: Host={dbConfig.Host}, Port={dbConfig.Port}, Name={dbConfig.Name}");

// Example 5: Configuration Benefits
Console.WriteLine("\n📌 Example 5: Configuration Integration Benefits");
Console.WriteLine("  ✅ Standard IConfiguration interface");
Console.WriteLine("  ✅ Compatible with ASP.NET Core");
Console.WriteLine("  ✅ Configuration providers chaining");
Console.WriteLine("  ✅ Section binding support");
Console.WriteLine("  ✅ Change notification support");

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("🎉 Configuration Integration sample completed successfully!");

// Clean up environment variable
Environment.SetEnvironmentVariable("MYAPP_Database__Host", null);

// Configuration binding class
public class DatabaseConfig
{
    public string Host { get; set; } = "";
    public string Port { get; set; } = "";
    public string Name { get; set; } = "";
}
