# Contributing to Sharingan

Thank you for your interest in contributing to Sharingan! This document provides guidelines and information for contributors.

## 🚀 Getting Started

### Prerequisites

-   [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later
-   A code editor (VS Code, Visual Studio, Rider, etc.)

### Building the Project

```bash
git clone https://github.com/Taiizor/Sharingan.git
cd Sharingan
dotnet build
```

### Running Tests

```bash
dotnet test
```

## 📝 How to Contribute

### Reporting Bugs

-   Use the [GitHub Issues](https://github.com/Taiizor/Sharingan/issues) page
-   Search existing issues before creating a new one
-   Include steps to reproduce, expected behavior, and actual behavior

### Suggesting Features

-   Open a GitHub Issue with the "enhancement" label
-   Describe the use case and proposed solution

### Pull Requests

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass (`dotnet test`)
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

## 🎨 Code Style

-   Follow the `.editorconfig` rules
-   Use meaningful names for variables and methods
-   Document public APIs with XML comments
-   Keep methods small and focused

## 📁 Project Structure

```
src/
├── Sharingan.Abstractions/    # Core interfaces
├── Sharingan/                  # Main library
├── Sharingan.Providers.*/      # Provider packages
└── Sharingan.Extensions.*/     # Extension packages
tests/
└── Sharingan.Tests/            # Unit tests
samples/
└── Sharingan.Sample.Console/   # Sample application
```

## 📜 License

By contributing, you agree that your contributions will be licensed under the MIT License.
