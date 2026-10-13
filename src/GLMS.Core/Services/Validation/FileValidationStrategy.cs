// File: Services/Validation/FileValidationStrategy.cs
// Purpose: Concrete Strategy that validates file-upload metadata against business
//          rules (PDF-only, maximum 10 MB). Ensures invalid files are rejected
//          before any bytes are written to storage.
// Implementation of the Strategy pattern (Gamma et al., 1994)
// Reference: Gamma, E., Helm, R., Johnson, R. and Vlissides, J. (1994)
//            Design Patterns: Elements of Reusable Object-Oriented Software.

using GLMS.Core.DTOs;
using GLMS.Core.Interfaces;

namespace GLMS.Core.Services.Validation;

/// <summary>
/// Validates <see cref="FileUploadRequest"/> metadata to enforce the system's
/// file-upload policy. Implements <see cref="IValidationStrategy{T}"/> for
/// <see cref="FileUploadRequest"/>.
/// <para>
/// Following the Strategy pattern (Gamma et al., 1994), this class isolates
/// file-validation rules so they can be unit-tested and swapped independently
/// of the <see cref="FileService"/> that orchestrates uploads.
/// </para>
/// </summary>
public class FileValidationStrategy : IValidationStrategy<FileUploadRequest>
{
    /// <summary>
    /// Maximum permitted file size (10 MB). Protects server storage and prevents
    /// denial-of-service via oversized uploads.
    /// </summary>
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    /// <inheritdoc />
    /// <remarks>
    /// Validates three constraints in order:
    /// 1. File name must be non-empty and have a <c>.pdf</c> extension.
    /// 2. MIME content type must be <c>application/pdf</c> to prevent extension spoofing.
    /// 3. File size must be between 1 byte and <see cref="MaxFileSize"/>.
    /// All violations are collected into a single <see cref="ValidationResult"/>.
    /// </remarks>
    public ValidationResult Validate(FileUploadRequest entity)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(entity.FileName))
        {
            result.AddError("File name is required.");
            return result;
        }

        var extension = Path.GetExtension(entity.FileName)?.ToLowerInvariant();
        if (extension != ".pdf")
        {
            result.AddError("Only PDF files (.pdf) are allowed.");
        }

        if (!string.Equals(entity.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            result.AddError("Content type must be application/pdf.");
        }

        if (entity.FileSize <= 0)
        {
            result.AddError("File is empty.");
        }
        else if (entity.FileSize > MaxFileSize)
        {
            result.AddError($"File size exceeds the maximum allowed size of {MaxFileSize / (1024 * 1024)} MB.");
        }

        return result;
    }
}
