<div align="center">

<p align="center">
  <img src="assets/Sharingan.png" alt="Sharingan Logo" width="128" height="128" />
  <h1>Sharingan</h1>
</p>

**Cross-Platform Local Settings Library for .NET**

[![NuGet](https://img.shields.io/nuget/v/Sharingan?style=for-the-badge&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Sharingan)
[![Downloads](https://img.shields.io/nuget/dt/Sharingan?style=for-the-badge&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Sharingan)
[![Build](https://img.shields.io/github/actions/workflow/status/Taiizor/Sharingan/build.yml?style=for-the-badge&logo=github&logoColor=white)](https://github.com/Taiizor/Sharingan/actions)
[![License](https://img.shields.io/github/license/Taiizor/Sharingan?style=for-the-badge)](LICENSE)

_A robust, multi-process-safe, async-first settings library for managing application, user, and device settings locally._

[Installation](#-installation) •
[Quick Start](#-quick-start) •
[Providers](#-providers) •
[Contributing](#-contributing)

</div>

---

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

---

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

---

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

---

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

---

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

---

## 🌐 Supported Platforms

| Platform | Supported | Notes                                                     |
| -------- | --------- | --------------------------------------------------------- |
| Windows  | ✅        | Full support including Registry provider                  |
| Linux    | ✅        | Full support via file-based providers                     |
| macOS    | ✅        | Full support via file-based providers                     |
| Android  | ✅        | Via .NET Standard / MAUI, uses internal app storage       |
| iOS      | ✅        | Via .NET Standard / MAUI, uses app sandbox Library folder |

> **Mobile Platform Notes:** On Android and iOS, settings are stored within the app's sandbox.
> Machine-scoped settings fall back to user-scoped storage due to platform sandboxing restrictions.

---

## 📁 Project Structure

```
Sharingan/
├── src/
│   ├── Sharingan.Abstractions/        # Core interfaces
│   ├── Sharingan/                     # Main library
│   ├── Sharingan.Providers.Registry/  # Windows Registry
│   ├── Sharingan.Providers.Ini/       # INI files
│   ├── Sharingan.Providers.Yaml/      # YAML files
│   ├── Sharingan.Providers.Xml/       # XML files
│   ├── Sharingan.Providers.Toml/      # TOML files
│   ├── Sharingan.Providers.SQLite/    # SQLite database
│   ├── Sharingan.Providers.Encrypted/ # Encryption wrapper
│   ├── Sharingan.Extensions.DependencyInjection/
│   └── Sharingan.Extensions.Configuration/
├── tests/
│   └── Sharingan.Tests/
├── samples/
└── Sharingan.sln
```

---

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/Taiizor/Sharingan.git
cd Sharingan

# Build
dotnet build

# Run tests
dotnet test
```

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

## 🔗 Links

-   [NuGet Package](https://www.nuget.org/packages/Sharingan)
-   [Issue Tracker](https://github.com/Taiizor/Sharingan/issues)
-   [Changelog](CHANGELOG.md)

---

<div align="center">

**Made with ❤️ by [Taiizor](https://github.com/Taiizor)**

</div>
