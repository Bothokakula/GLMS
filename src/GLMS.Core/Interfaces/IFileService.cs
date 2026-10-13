// File: Interfaces/IFileService.cs
// Purpose: Abstracts file storage and validation so the API layer is not coupled
//          to a specific storage mechanism (local disk, cloud blob, etc.).
// Reference: Asynchronous programming with async and await —
//            Microsoft (2024) Asynchronous programming with async and await.

using GLMS.Core.DTOs;

namespace GLMS.Core.Interfaces;

/// <summary>
/// Service contract for uploading, validating, and retrieving files
/// (specifically signed agreement PDFs attached to contracts).
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Persists the contents of <paramref name="stream"/> to storage under a
    /// unique name derived from <paramref name="fileName"/>.
    /// </summary>
    /// <param name="stream">The file content stream to save.</param>
    /// <param name="fileName">The original file name (used for its extension).</param>
    /// <returns>The storage path that can later be passed to <see cref="GetFileAsync"/>.</returns>
    Task<string> SaveFileAsync(Stream stream, string fileName);

    /// <summary>
    /// Validates file metadata (name, MIME type, size) against business rules
    /// (PDF-only, max 10 MB) without reading the file stream.
    /// </summary>
    /// <param name="fileName">Original file name including extension.</param>
    /// <param name="contentType">MIME content type reported by the client.</param>
    /// <param name="size">File size in bytes.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating pass or fail with reasons.</returns>
    ValidationResult ValidateFile(string fileName, string contentType, long size);

    /// <summary>
    /// Retrieves a previously saved file as a readable stream.
    /// </summary>
    /// <param name="path">The storage path returned by <see cref="SaveFileAsync"/>.</param>
    /// <returns>
    /// A tuple of the file stream and its content type, or <c>null</c> if the file
    /// does not exist at the given path.
    /// </returns>
    Task<(Stream Stream, string ContentType)?> GetFileAsync(string path);
}
