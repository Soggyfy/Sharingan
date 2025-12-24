# Security Policy

## Supported Versions

The following versions of Sharingan are currently supported with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security issue, please follow these steps:

### Do NOT

-   ❌ Open a public GitHub issue
-   ❌ Discuss the vulnerability publicly before it's fixed
-   ❌ Share exploit code publicly

### Do

1. **Email the maintainer directly** at the email address listed in the [NuGet package](https://www.nuget.org/packages/Sharingan) or contact via [GitHub profile](https://github.com/Taiizor)

2. **Include the following information:**

    - Type of vulnerability
    - Affected package(s) and version(s)
    - Steps to reproduce
    - Potential impact
    - Any suggested fixes (optional)

3. **Wait for a response** - We aim to acknowledge reports within 48 hours

### What to Expect

-   **Acknowledgment:** Within 48 hours of your report
-   **Initial Assessment:** Within 7 days
-   **Fix Timeline:** Depends on severity and complexity
-   **Credit:** We will credit you in the security advisory (unless you prefer to remain anonymous)

### Severity Levels

| Level    | Description                                   | Response Time |
| -------- | --------------------------------------------- | ------------- |
| Critical | Remote code execution, data breach            | 24-48 hours   |
| High     | Privilege escalation, sensitive data exposure | 7 days        |
| Medium   | Limited impact vulnerabilities                | 14 days       |
| Low      | Minor issues, defense-in-depth improvements   | 30 days       |

## Security Best Practices

When using Sharingan in your applications:

### Encryption Provider

-   Always use strong encryption keys (minimum 256 bits for AES)
-   Store encryption keys securely (use environment variables or secure key vaults)
-   Consider using DPAPI on Windows for machine-specific encryption

### File-Based Providers

-   Ensure proper file permissions on settings files
-   Avoid storing sensitive data in plain text
-   Use the Encrypted provider wrapper for sensitive settings

### Registry Provider (Windows)

-   Use appropriate registry hives based on sensitivity
-   Be aware of permission implications with HKLM vs HKCU

## Security Updates

Security updates are released as patch versions (e.g., 1.0.1 → 1.0.2). Subscribe to the repository's releases to be notified of security updates.
