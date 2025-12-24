using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sharingan.Abstractions;
using Sharingan.Extensions.DependencyInjection;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("🔮 Sharingan - Dependency Injection Sample");
Console.WriteLine(new string('=', 50));

// Example 1: Basic DI Registration
Console.WriteLine("\n📌 Example 1: Basic DI Registration");
ServiceCollection services = new();

services.AddSharingan(builder => builder.WithApplicationName("DIApp")
           .UseJsonFile("di-settings.json")
           .UseInMemory(priority: 100));

ServiceProvider provider = services.BuildServiceProvider();
ISettingsStore settings = provider.GetRequiredService<ISettingsStore>();

settings.Set("app.name", "DI Sample Application");
settings.Set("app.version", "1.0.0");

Console.WriteLine($"  App Name: {settings.Get("app.name", "")}");
Console.WriteLine($"  App Version: {settings.Get("app.version", "")}");

// Example 2: Simple Registration with Application Name
Console.WriteLine("\n📌 Example 2: Simple Registration");
ServiceCollection simpleServices = new();
simpleServices.AddSharingan("MySimpleApp", "Taiizor");

ServiceProvider simpleProvider = simpleServices.BuildServiceProvider();
ISettingsStore simpleSettings = simpleProvider.GetRequiredService<ISettingsStore>();

simpleSettings.Set("theme", "dark");
Console.WriteLine($"  Theme: {simpleSettings.Get("theme", "")}");

// Example 3: Strongly-Typed Options Pattern
Console.WriteLine("\n📌 Example 3: Options Pattern Integration");
ServiceCollection optionsServices = new();

optionsServices.AddSharingan(builder => builder.WithApplicationName("OptionsApp")
           .UseJsonFile("options-settings.json"));

optionsServices.AddSharinganOptions<AppSettings>("App");
optionsServices.AddSharinganOptions<DatabaseSettings>("Database");

// Pre-populate settings
ServiceProvider optionsProvider = optionsServices.BuildServiceProvider();
ISettingsStore optionsStore = optionsProvider.GetRequiredService<ISettingsStore>();
optionsStore.Set("App", new AppSettings { Name = "My Application", Debug = true });
optionsStore.Set("Database", new DatabaseSettings { Host = "localhost", Port = 5432 });

// Retrieve as IOptions<T>
IOptions<AppSettings> appOptions = optionsProvider.GetRequiredService<IOptions<AppSettings>>();
IOptions<DatabaseSettings> dbOptions = optionsProvider.GetRequiredService<IOptions<DatabaseSettings>>();

Console.WriteLine($"  App Settings: Name={appOptions.Value.Name}, Debug={appOptions.Value.Debug}");
Console.WriteLine($"  DB Settings: Host={dbOptions.Value.Host}, Port={dbOptions.Value.Port}");

// Example 4: Service Injection Demo
Console.WriteLine("\n📌 Example 4: Service Injection Demo");
ServiceCollection demoServices = new();
demoServices.AddSharingan("DemoApp");
demoServices.AddScoped<MyService>();

ServiceProvider demoProvider = demoServices.BuildServiceProvider();
MyService myService = demoProvider.GetRequiredService<MyService>();
myService.DoWork();

// Example 5: DI Benefits
Console.WriteLine("\n📌 Example 5: DI Integration Benefits");
Console.WriteLine("  ✅ Singleton settings store");
Console.WriteLine("  ✅ Constructor injection support");
Console.WriteLine("  ✅ IOptions<T> pattern integration");
Console.WriteLine("  ✅ Testability with mock stores");
Console.WriteLine("  ✅ Lifetime management by container");

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("🎉 Dependency Injection sample completed successfully!");

// Settings classes for Options pattern
public class AppSettings
{
    public string Name { get; set; } = "";
    public bool Debug { get; set; }
}

public class DatabaseSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
}

// Example service that uses ISettingsStore
public class MyService
{
    private readonly ISettingsStore _settings;

    public MyService(ISettingsStore settings)
    {
        _settings = settings;
    }

    public void DoWork()
    {
        _settings.Set("lastWorkTime", DateTime.UtcNow.ToString("O"));
        Console.WriteLine($"  MyService working... Last run: {_settings.Get("lastWorkTime", "")}");
    }
}
