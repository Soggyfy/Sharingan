namespace Sharingan.Internal;

/// <summary>
/// Provides helper methods for argument validation, throwing appropriate exceptions
/// when validation fails. Uses platform-specific APIs when available for better
/// performance and standardized exception messages.
/// </summary>
/// <remarks>
/// <para>
/// This class provides consistent argument validation across all .NET target frameworks.
/// On modern .NET versions (7.0+), it delegates to built-in ArgumentException.ThrowIf*
/// methods for optimal performance and standardized messages. On older frameworks,
/// it provides equivalent functionality with manual implementations.
/// </para>
/// <para>
/// All methods in this class are designed to return the validated value, enabling
/// fluent validation patterns in constructors and property setters.
/// </para>
/// </remarks>
public static class ThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the specified value is null.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate. Must be a reference type.</typeparam>
    /// <param name="value">The value to validate for null.</param>
    /// <param name="parameterName">The name of the parameter being validated, used in the exception message.</param>
    /// <returns>The validated value if it is not null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <remarks>
    /// On .NET 7.0+, this method uses <c>ArgumentNullException.ThrowIfNull</c> for optimal performance.
    /// On older frameworks, it performs a manual null check.
    /// </remarks>
    /// <example>
    /// <code>
    /// public MyClass(ISomeService service)
    /// {
    ///     _service = ThrowHelper.ThrowIfNull(service, nameof(service));
    /// }
    /// </code>
    /// </example>
    public static T ThrowIfNull<T>(T? value, string? parameterName = null) where T : class
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(value, parameterName);
        return value;
#else
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }
        return value;
#endif
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if the specified string is null or empty.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="parameterName">The name of the parameter being validated, used in the exception message.</param>
    /// <returns>The validated string if it is not null or empty.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or an empty string.</exception>
    /// <remarks>
    /// On .NET 7.0+, this method uses <c>ArgumentException.ThrowIfNullOrEmpty</c> for optimal performance.
    /// On older frameworks, it performs a manual check using <c>string.IsNullOrEmpty</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// public void SetKey(string key)
    /// {
    ///     _key = ThrowHelper.ThrowIfNullOrEmpty(key, nameof(key));
    /// }
    /// </code>
    /// </example>
    public static string ThrowIfNullOrEmpty(string? value, string? parameterName = null)
    {
#if NET7_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(value, parameterName);
        return value;
#else
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Value cannot be null or empty.", parameterName);
        }
        return value;
#endif
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if the specified string is null, empty, or consists only of whitespace.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="parameterName">The name of the parameter being validated, used in the exception message.</param>
    /// <returns>The validated string if it is not null, empty, or whitespace.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null, empty, or consists only of whitespace characters.</exception>
    /// <remarks>
    /// On .NET 8.0+, this method uses <c>ArgumentException.ThrowIfNullOrWhiteSpace</c> for optimal performance.
    /// On older frameworks, it performs a manual check using <c>string.IsNullOrWhiteSpace</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// public void SetName(string name)
    /// {
    ///     _name = ThrowHelper.ThrowIfNullOrWhiteSpace(name, nameof(name));
    /// }
    /// </code>
    /// </example>
    public static string ThrowIfNullOrWhiteSpace(string? value, string? parameterName = null)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
#else
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }
        return value!;
#endif
    }
}
