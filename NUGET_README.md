# Sharingan

**Cross-Platform Local Settings Library for .NET**

A robust, multi-process-safe, async-first settings library for managing application, user, and device settings locally.

## ✨ Features

-   🌍 **Cross-Platform** — Works on Windows, Linux, macOS, Android, and iOS
-   🔒 **Multi-Process Safe** — File locking and atomic writes prevent data corruption
-   ⚡ **Async-First** — Full async/await support with CancellationToken
-   🔌 **11 Pluggable Providers** — JSON, Registry, INI, YAML, XML, TOML, SQLite, and more
-   🏗️ **Composite Configuration** — Chain multiple providers with priority-based resolution
-   🔐 **Encryption Support** — AES-256-GCM and DPAPI for sensitive settings
-   📦 **Lightweight** — Minimal dependencies, optimized for performance
-   🎯 **Multi-Target** — Supports .NET Framework 4.8+, .NET Standard 2.0+, and .NET 7-10
-   🔔 **Change Notifications** — Event-driven updates when settings change
-   💪 **Strongly-Typed** — Full generic support with type-safe access
-   🔗 **DI Integration** — Microsoft.Extensions.DependencyInjection support
-   🌐 **Configuration Bridge** — Works with Microsoft.Extensions.Configuration

## 📦 Installation

### Core Package

```bash
dotnet add package Sharingan
```

### Provider Packages

```bash
# Windows Registry support
dotnet add package Sharingan.Providers.Registry

# INI file support
dotnet add package Sharingan.Providers.Ini

# YAML file support
dotnet add package Sharingan.Providers.Yaml

# XML file support
dotnet add package Sharingan.Providers.Xml

# TOML file support
dotnet add package Sharingan.Providers.Toml

# SQLite database support
dotnet add package Sharingan.Providers.SQLite

# Encryption support
dotnet add package Sharingan.Providers.Encrypted
```

### Extension Packages

```bash
# Dependency Injection integration
dotnet add package Sharingan.Extensions.DependencyInjection

# Microsoft.Extensions.Configuration bridge
dotnet add package Sharingan.Extensions.Configuration
```

## 🚀 Quick Start

### Basic Usage

```csharp
using Sharingan;

// Store settings
Settings.Default.Set("app.theme", "Dark");
Settings.Default.Set("app.fontSize", 14);
Settings.Default.Set("app.autoSave", true);

// Retrieve settings with type-safe access
string theme = Settings.Default.GetString("app.theme");
int fontSize = Settings.Default.GetInt("app.fontSize");
bool autoSave = Settings.Default.GetBool("app.autoSave");

// Or use generic Get<T>
var config = Settings.Default.Get<AppConfig>("app.config", new AppConfig());
```

### Using the Builder Pattern

```csharp
using Sharingan;
using Sharingan.Providers;

ISettingsStore settings = new SharinganBuilder()
    .WithApplicationName("MyApp")
    .WithOrganizationName("MyCompany")
    .UseEnvironmentVariables("MYAPP_", priority: 100)  // Highest priority
    .UseJsonFile("settings.json", SettingsScope.User, priority: 50)
    .UseInMemory(priority: 10)  // Fallback
    .Build();

// Environment variables override JSON file settings
settings.Set("database.host", "localhost");
string host = settings.GetString("database.host");
```

### Dependency Injection

```csharp
using Sharingan.Extensions.DependencyInjection;

services.AddSharingan(builder => builder
    .WithApplicationName("MyApp")
    .UseJsonFile("settings.json")
);

// Inject anywhere
public class MyService(ISettingsStore settings)
{
    public string GetTheme() => settings.GetString("theme", "Light");
}
```

## 🔧 Providers

| Provider    | Package                | Description                     | Cross-Platform |
| ----------- | ---------------------- | ------------------------------- | -------------- |
| JSON        | `Sharingan`            | Default file-based storage      | ✅             |
| InMemory    | `Sharingan`            | Session-scoped, non-persistent  | ✅             |
| Environment | `Sharingan`            | Read-only environment variables | ✅             |
| Composite   | `Sharingan`            | Chain multiple providers        | ✅             |
| Registry    | `.Providers.Registry`  | Windows Registry storage        | ❌ Windows     |
| INI         | `.Providers.Ini`       | INI file format                 | ✅             |
| YAML        | `.Providers.Yaml`      | YAML file format                | ✅             |
| XML         | `.Providers.Xml`       | XML file format                 | ✅             |
| TOML        | `.Providers.Toml`      | TOML file format                | ✅             |
| SQLite      | `.Providers.SQLite`    | Database storage                | ✅             |
| Encrypted   | `.Providers.Encrypted` | Encryption wrapper              | ✅             |

## 🎯 Target Frameworks

| Framework            | Supported |
| -------------------- | --------- |
| .NET Framework 4.8   | ✅        |
| .NET Framework 4.8.1 | ✅        |
| .NET Standard 2.0    | ✅        |
| .NET Standard 2.1    | ✅        |
| .NET 7.0             | ✅        |
| .NET 8.0             | ✅        |
| .NET 9.0             | ✅        |
| .NET 10.0            | ✅        |

## 🌐 Supported Platforms

| Platform | Supported | Notes                                                     |
| -------- | --------- | --------------------------------------------------------- |
| Windows  | ✅        | Full support including Registry provider                  |
| Linux    | ✅        | Full support via file-based providers                     |
| macOS    | ✅        | Full support via file-based providers                     |
| Android  | ✅        | Via .NET Standard / MAUI, uses internal app storage       |
| iOS      | ✅        | Via .NET Standard / MAUI, uses app sandbox Library folder |

## 📄 License

This project is licensed under the **MIT License**.

## 🔗 Links

-   [GitHub Repository](https://github.com/Taiizor/Sharingan)
-   [Issue Tracker](https://github.com/Taiizor/Sharingan/issues)
-   [Changelog](https://github.com/Taiizor/Sharingan/blob/develop/CHANGELOG.md)
