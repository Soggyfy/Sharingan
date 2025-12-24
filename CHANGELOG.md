# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

-   Initial release of Sharingan
-   Core abstractions: `ISettingsStore`, `ISettingsProvider`, `ISettingsSerializer`
-   JSON file provider with atomic writes and file watching
-   In-memory provider for session-scoped settings
-   Environment variables provider (read-only)
-   Composite provider for chaining multiple providers
-   Windows Registry provider
-   INI file provider
-   YAML file provider (using YamlDotNet)
-   XML file provider
-   TOML file provider (using Tomlyn)
-   SQLite database provider
-   Encrypted provider with AES-256-GCM support
-   DI integration with `Microsoft.Extensions.DependencyInjection`
-   Configuration bridge with `Microsoft.Extensions.Configuration`
-   Multi-targeting: net48, net481, netstandard2.0, netstandard2.1, net7.0, net8.0, net9.0, net10.0
-   Fluent builder pattern with `SharinganBuilder`
-   Change notifications via `SettingsChanged` event
-   Extension methods for common operations
