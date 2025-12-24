namespace Sharingan.Abstractions;

/// <summary>
/// Provides serialization and deserialization of values for settings storage.
/// Implementations of this interface are responsible for converting between typed
/// objects and their string representation for persistent storage.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ISettingsSerializer"/> interface abstracts the serialization layer,
/// allowing different serialization formats (JSON, XML, YAML, etc.) to be used with
/// any settings provider. The default implementation uses System.Text.Json for
/// cross-platform compatibility and performance.
/// </para>
/// <para>
/// Custom serializers can be provided to:
/// <list type="bullet">
/// <item><description>Use alternative serialization formats (e.g., MessagePack, Protocol Buffers)</description></item>
/// <item><description>Apply custom serialization rules (e.g., encryption, compression)</description></item>
/// <item><description>Handle special types that require custom conversion logic</description></item>
/// <item><description>Maintain compatibility with legacy configuration formats</description></item>
/// </list>
/// </para>
/// <para>
/// Implementations should be thread-safe and reusable across multiple operations.
/// Serialization errors should throw meaningful exceptions with details about the
/// type and value that could not be serialized.
/// </para>
/// </remarks>
/// <example>
/// Using a custom serializer:
/// <code>
/// public class CustomSerializer : ISettingsSerializer
/// {
///     public string Serialize&lt;T&gt;(T value) => JsonSerializer.Serialize(value, _options);
///     
///     public T? Deserialize&lt;T&gt;(string value) => JsonSerializer.Deserialize&lt;T&gt;(value, _options);
///     
///     public object? Deserialize(string value, Type type) => JsonSerializer.Deserialize(value, type, _options);
/// }
/// 
/// var store = new SharinganBuilder()
///     .WithSerializer(new CustomSerializer())
///     .UseJsonFile()
///     .Build();
/// </code>
/// </example>
public interface ISettingsSerializer
{
    /// <summary>
    /// Serializes a value to its string representation for storage.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize. Can be any type that the serializer supports, including primitives, complex objects, collections, and nested structures.</typeparam>
    /// <param name="value">The value to serialize. Can be null for nullable types.</param>
    /// <returns>A string representation of the value that can be stored and later deserialized back to the original type. Returns an appropriate representation for null values (e.g., "null" for JSON).</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be serialized due to an unsupported type or circular reference.</exception>
    /// <remarks>
    /// <para>
    /// The serialization format should be consistent and deterministic - serializing the same
    /// value multiple times should produce identical output (when possible).
    /// </para>
    /// <para>
    /// For primitive types (int, bool, string, etc.), implementations may choose to use
    /// simple ToString() conversion or full serialization format depending on the implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var settings = new AppSettings { Theme = "dark", Volume = 75 };
    /// string json = serializer.Serialize(settings);
    /// // json: {"theme":"dark","volume":75}
    /// </code>
    /// </example>
    string Serialize<T>(T value);

    /// <summary>
    /// Deserializes a string representation back to the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to. Must be compatible with the serialized data structure.</typeparam>
    /// <param name="value">The serialized string representation to deserialize. Can be null or empty, which typically results in a default value being returned.</param>
    /// <returns>The deserialized value of type <typeparamref name="T"/>, or the default value for the type if the input is null, empty, or cannot be deserialized.</returns>
    /// <exception cref="FormatException">Thrown when the value is not in a valid format for the target type.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be converted to the specified type.</exception>
    /// <remarks>
    /// <para>
    /// Implementations should handle common edge cases gracefully:
    /// <list type="bullet">
    /// <item><description>Null input should return default(T)</description></item>
    /// <item><description>Empty string should return default(T)</description></item>
    /// <item><description>Type mismatches should attempt conversion or throw a meaningful exception</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string json = "{\"theme\":\"dark\",\"volume\":75}";
    /// var settings = serializer.Deserialize&lt;AppSettings&gt;(json);
    /// // settings.Theme == "dark", settings.Volume == 75
    /// </code>
    /// </example>
    T? Deserialize<T>(string value);

    /// <summary>
    /// Deserializes a string representation to the specified runtime type.
    /// </summary>
    /// <param name="value">The serialized string representation to deserialize. Can be null or empty.</param>
    /// <param name="type">The target Type to deserialize to. This type must be compatible with the serialized data structure.</param>
    /// <returns>The deserialized value as an object, or null if the input is null, empty, or cannot be deserialized.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    /// <exception cref="FormatException">Thrown when the value is not in a valid format for the target type.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be converted to the specified type.</exception>
    /// <remarks>
    /// <para>
    /// This non-generic overload is useful when the target type is only known at runtime,
    /// such as when using reflection or when deserializing to a dynamically determined type.
    /// </para>
    /// <para>
    /// Implementations should handle the same edge cases as the generic <see cref="Deserialize{T}"/> method.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Type settingsType = typeof(AppSettings);
    /// string json = "{\"theme\":\"dark\"}";
    /// object? settings = serializer.Deserialize(json, settingsType);
    /// </code>
    /// </example>
    object? Deserialize(string value, Type type);
}
