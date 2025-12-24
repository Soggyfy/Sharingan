using System.Text;

namespace Sharingan.Internal;

/// <summary>
/// Provides cross-framework file operation helpers that work consistently across all
/// supported .NET target frameworks including .NET Framework 4.8+, .NET Standard 2.0/2.1,
/// and modern .NET versions.
/// </summary>
/// <remarks>
/// <para>
/// This helper class abstracts away differences between .NET framework versions for common
/// file operations. Modern .NET versions have built-in async file I/O methods, while older
/// frameworks require manual implementation using streams.
/// </para>
/// <para>
/// All methods in this class are thread-safe and handle proper encoding (UTF-8) consistently.
/// </para>
/// </remarks>
public static class FileHelper
{
    /// <summary>
    /// Asynchronously writes a string to a file, creating the file if it doesn't exist
    /// or overwriting it if it does. Uses UTF-8 encoding without BOM.
    /// </summary>
    /// <param name="path">The full path of the file to write. The directory must exist.</param>
    /// <param name="contents">The string content to write to the file.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the write operation.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    /// <exception cref="IOException">Thrown when an I/O error occurs while writing to the file.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the cancellation token.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have permission to write to the file.</exception>
    /// <remarks>
    /// On modern .NET (Core 3.0+ and .NET 5+), this method uses the built-in
    /// <c>File.WriteAllTextAsync</c>. On older frameworks, it uses a manual implementation
    /// with async file streams for true non-blocking I/O.
    /// </remarks>
    public static async Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        await File.WriteAllTextAsync(path, contents, cancellationToken).ConfigureAwait(false);
#else
        cancellationToken.ThrowIfCancellationRequested();
        byte[] bytes = Encoding.UTF8.GetBytes(contents);
        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
#endif
    }

    /// <summary>
    /// Asynchronously reads all text from a file using UTF-8 encoding.
    /// </summary>
    /// <param name="path">The full path of the file to read. The file must exist.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the read operation.</param>
    /// <returns>A task representing the asynchronous read operation, containing the file contents as a string.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading the file.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the cancellation token.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have permission to read the file.</exception>
    /// <remarks>
    /// On modern .NET (Core 3.0+ and .NET 5+), this method uses the built-in
    /// <c>File.ReadAllTextAsync</c>. On older frameworks, it uses a manual implementation
    /// with async file streams and StreamReader.
    /// </remarks>
    public static async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
#else
        cancellationToken.ThrowIfCancellationRequested();
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        using StreamReader reader = new(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
#endif
    }

    /// <summary>
    /// Asynchronously reads all lines from a file using UTF-8 encoding.
    /// </summary>
    /// <param name="path">The full path of the file to read. The file must exist.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the read operation.</param>
    /// <returns>A task representing the asynchronous read operation, containing an array of all lines in the file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading the file.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the cancellation token.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have permission to read the file.</exception>
    /// <remarks>
    /// On modern .NET (Core 3.0+ and .NET 5+), this method uses the built-in
    /// <c>File.ReadAllLinesAsync</c>. On older frameworks, it reads lines one at a time
    /// using StreamReader.ReadLineAsync().
    /// </remarks>
    public static async Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        return await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false);
#else
        cancellationToken.ThrowIfCancellationRequested();
        List<string> lines = [];
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        using StreamReader reader = new(stream, Encoding.UTF8);
        string? line;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            lines.Add(line);
        }
        return [.. lines];
#endif
    }

    /// <summary>
    /// Moves a file from source to destination with overwrite support.
    /// If the destination file already exists, it is deleted before the move.
    /// </summary>
    /// <param name="sourceFileName">The full path of the file to move.</param>
    /// <param name="destFileName">The full path of the destination. Any existing file at this path will be overwritten.</param>
    /// <exception cref="FileNotFoundException">Thrown when the source file does not exist.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs during the move operation.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have permission to perform the operation.</exception>
    /// <remarks>
    /// <para>
    /// On modern .NET (Core 3.0+ and .NET 5+), this method uses the built-in
    /// <c>File.Move(source, dest, overwrite: true)</c>. On older frameworks, it manually
    /// deletes the destination file if it exists before performing the move.
    /// </para>
    /// <para>
    /// This method is used primarily for atomic writes: write to a temporary file first,
    /// then move/rename to the final destination. This prevents data corruption if the
    /// write operation is interrupted.
    /// </para>
    /// </remarks>
    public static void MoveWithOverwrite(string sourceFileName, string destFileName)
    {
#if NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        File.Move(sourceFileName, destFileName, overwrite: true);
#else
        if (File.Exists(destFileName))
        {
            File.Delete(destFileName);
        }
        File.Move(sourceFileName, destFileName);
#endif
    }
}
