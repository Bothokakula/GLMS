// File: Services/FileService.cs
// Purpose: Handles local-disk file storage for signed agreement PDFs.
//          Validates uploads (PDF-only, max 10 MB), persists files with GUID-based
//          names to avoid collisions, and serves them back for download.
// Reference: Asynchronous programming with async and await —
//            Microsoft (2024) Asynchronous programming with async and await.

using GLMS.Core.DTOs;
using GLMS.Core.Interfaces;

namespace GLMS.Core.Services;

/// <summary>
/// Local-disk implementation of <see cref="IFileService"/>. Files are saved
/// under a configurable upload directory with GUID-based names so that
/// concurrent uploads never collide, even if the original file names are identical.
/// </summary>
public class FileService : IFileService
{
    private readonly string _uploadPath;

    /// <summary>
    /// Initialises a new instance of <see cref="FileService"/> and ensures the
    /// upload directory exists on disk.
    /// </summary>
    /// <param name="uploadPath">
    /// Root directory where uploaded files are stored. Defaults to "uploads"
    /// relative to the working directory.
    /// </param>
    public FileService(string uploadPath = "uploads")
    {
        _uploadPath = uploadPath;
        Directory.CreateDirectory(_uploadPath);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The original file extension is preserved so the stored file can still be
    /// identified by type, but the base name is replaced with a GUID to guarantee
    /// uniqueness. The async stream copy avoids loading the entire file into memory.
    /// </remarks>
    public async Task<string> SaveFileAsync(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadPath, uniqueName);

        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fileStream);

        return filePath;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Enforces three rules in a single pass:
    /// 1. File extension must be <c>.pdf</c>.
    /// 2. MIME type must be <c>application/pdf</c> (prevents extension spoofing).
    /// 3. Size must be between 1 byte and 10 MB.
    /// All failures are collected into one <see cref="ValidationResult"/> so the
    /// caller can display every issue at once.
    /// </remarks>
    public ValidationResult ValidateFile(string fileName, string contentType, long size)
    {
        var result = new ValidationResult();
        const long maxSize = 10 * 1024 * 1024; // 10 MB

        if (string.IsNullOrWhiteSpace(fileName))
        {
            result.AddError("File name is required.");
            return result;
        }

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (extension != ".pdf")
        {
            result.AddError("Only PDF files are allowed.");
        }

        if (!string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            result.AddError("Content type must be application/pdf.");
        }

        if (size <= 0)
        {
            result.AddError("File is empty.");
        }
        else if (size > maxSize)
        {
            result.AddError($"File size exceeds the maximum allowed size of {maxSize / (1024 * 1024)} MB.");
        }

        return result;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns a synchronous result wrapped in <see cref="Task.FromResult{T}"/>
    /// because local file-existence checks are fast and do not benefit from true
    /// async I/O. The caller is responsible for disposing the returned stream.
    /// </remarks>
    public Task<(Stream Stream, string ContentType)?> GetFileAsync(string path)
    {
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(_uploadPath, path);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<(Stream Stream, string ContentType)?>(null);
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<(Stream Stream, string ContentType)?>((stream, "application/pdf"));
    }
}
