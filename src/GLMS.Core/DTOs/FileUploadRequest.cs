// File: DTOs/FileUploadRequest.cs
// Purpose: Metadata describing a file being uploaded. Used by the Strategy pattern
//          validation (FileValidationStrategy) to enforce business rules (PDF-only,
//          max 10 MB) before the file bytes are persisted to disk.
// Reference: Strategy pattern — Gamma, E., Helm, R., Johnson, R. and Vlissides, J.
//            (1994) Design Patterns: Elements of Reusable Object-Oriented Software.

namespace GLMS.Core.DTOs;

/// <summary>
/// Describes the metadata of a file upload so validation strategies can verify
/// file type, MIME type, and size constraints without reading the file stream.
/// </summary>
public class FileUploadRequest
{
    /// <summary>
    /// Original file name including extension (e.g. "contract.pdf").
    /// Used to verify the file extension is permitted.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type reported by the client (e.g. "application/pdf").
    /// Validated to prevent extension-spoofing attacks.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Size of the file in bytes. Checked against the maximum allowed size
    /// to protect server storage and prevent denial-of-service via large uploads.
    /// </summary>
    public long FileSize { get; set; }
}
