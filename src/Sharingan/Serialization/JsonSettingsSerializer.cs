using Sharingan.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Sharingan.Serialization;

/// <summary>
/// Default JSON serializer implementation using System.Text.Json.
/// Provides serialization and deserialization of values for settings storage with
/// sensible defaults optimized for configuration scenarios.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="JsonSettingsSerializer"/> class implements <see cref="ISettingsSerializer"/>
/// using the high-performance System.Text.Json library. It is configured with defaults
/// suitable for configuration files:
/// <list type="bullet">
/// <item><description>Pretty-printed output (WriteIndented = true) for human readability</description></item>
/// <item><description>CamelCase property naming for JavaScript/JSON conventions</description></item>
/// <item><description>Null values ignored when writing to reduce file size</description></item>
/// <item><description>Comments allowed when reading for annotated config files</description></item>
/// <item><description>Trailing commas allowed for easier manual editing</description></item>
/// </list>
/// </para>
/// <para>
/// A <see cref="Default"/> singleton instance is provided for common use cases.
/// Custom instances can be created with specific <see cref="JsonSerializerOptions"/>.
/// </para>
/// </remarks>
/// <example>
/// Using the default serializer:
/// <code>
/// var serializer = JsonSettingsSerializer.Default;
/// var json = serializer.Serialize(new { Name = "Test", Value = 42 });
/// var obj = serializer.Deserialize&lt;MyClass&gt;(json);
/// </code>
/// </example>
/// <example>
/// Using a custom serializer with specific options:
/// <code>
/// var options = new JsonSerializerOptions
/// {
///     PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
///     WriteIndented = false
/// };
/// var serializer = new JsonSettingsSerializer(options);
/// </code>
/// </example>
/// <seealso cref="ISettingsSerializer"/>
public class JsonSettingsSerializer : ISettingsSerializer
{
    /// <summary>
    /// The JSON serializer options used for all serialization operations.
    /// </summary>
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Gets the default singleton instance of <see cref="JsonSettingsSerializer"/>
    /// with standard options optimized for configuration files.
    /// </summary>
    /// <value>A thread-safe singleton instance with default configuration.</value>
    /// <remarks>
    /// The default instance uses the following settings:
    /// <list type="bullet">
    /// <item><description><c>WriteIndented = true</c>: Pretty-printed output</description></item>
    /// <item><description><c>PropertyNamingPolicy = CamelCase</c>: Standard JSON naming</description></item>
    /// <item><description><c>DefaultIgnoreCondition = WhenWritingNull</c>: Omit null values</description></item>
    /// <item><description><c>ReadCommentHandling = Skip</c>: Allow comments in input</description></item>
    /// <item><description><c>AllowTrailingCommas = true</c>: Lenient parsing</description></item>
    /// </list>
    /// </remarks>
    public static JsonSettingsSerializer Default { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSettingsSerializer"/> class
    /// with optional custom JSON serializer options.
    /// </summary>
    /// <param name="options">Custom JSON serializer options to use. If null, default options are created.</param>
    /// <remarks>
    /// If no options are provided, the serializer uses default options optimized for
    /// configuration file serialization. See <see cref="CreateDefaultOptions"/> for details.
    /// </remarks>
    public JsonSettingsSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? CreateDefaultOptions();
    }

    /// <summary>
    /// Serializes a value to its JSON string representation.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize. Can be null for nullable types.</param>
    /// <returns>A JSON string representation of the value.</returns>
    /// <exception cref="NotSupportedException">Thrown when the type is not supported for serialization.</exception>
    /// <remarks>
    /// The serialization uses the configured <see cref="JsonSerializerOptions"/>, including
    /// property naming policy and null handling settings.
    /// </remarks>
    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, _options);
    }

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="value">The JSON string to deserialize. Can be null or empty.</param>
    /// <returns>The deserialized value, or default(T) if the input is null or empty.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to the target type.</exception>
    /// <remarks>
    /// Empty or null strings return default(T) without throwing an exception, which is
    /// convenient for handling missing or optional settings values.
    /// </remarks>
    public T? Deserialize<T>(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value, _options);
    }

    /// <summary>
    /// Deserializes a JSON string to the specified runtime type.
    /// </summary>
    /// <param name="value">The JSON string to deserialize. Can be null or empty.</param>
    /// <param name="type">The target Type to deserialize to.</param>
    /// <returns>The deserialized value as an object, or null if the input is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to the target type.</exception>
    /// <remarks>
    /// This non-generic overload is useful when the target type is only known at runtime.
    /// Empty or null strings return null without throwing an exception.
    /// </remarks>
    public object? Deserialize(string value, Type type)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return JsonSerializer.Deserialize(value, type, _options);
    }

    /// <summary>
    /// Creates the default JSON serializer options optimized for configuration file serialization.
    /// </summary>
    /// <returns>A new <see cref="JsonSerializerOptions"/> instance with default settings.</returns>
    private static JsonSerializerOptions CreateDefaultOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
#if NET7_0_OR_GREATER
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
#endif
        };
    }
}
