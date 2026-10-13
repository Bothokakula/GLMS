// FileValidationTests.cs
// Unit tests for the FileValidationStrategy, which enforces file upload constraints:
//   - Only PDF files are accepted (extension + content type)
//   - File size must be between 1 byte and 10 MB
//   - File name must not be null or empty
// xUnit test framework (xUnit.net, 2024)

using GLMS.Core.DTOs;
using GLMS.Core.Services.Validation;

namespace GLMS.Tests.Unit;

/// <summary>
/// Tests the <see cref="FileValidationStrategy"/> to ensure only valid PDF files
/// within the size limits are accepted for contract agreement uploads.
/// </summary>
public class FileValidationTests
{
    private readonly FileValidationStrategy _strategy = new();

    /// <summary>
    /// Validates the happy path: a correctly-named PDF with valid content type and small file size passes validation.
    /// </summary>
    [Fact]
    public void Validate_PdfFile_ReturnsValid()
    {
        // Follows Arrange-Act-Assert testing pattern
        var request = new FileUploadRequest
        {
            FileName = "contract.pdf",
            ContentType = "application/pdf",
            FileSize = 1024
        };

        var result = _strategy.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// Validates that executable files are rejected to prevent malicious uploads.
    /// </summary>
    [Fact]
    public void Validate_ExeFile_ReturnsInvalid()
    {
        var request = new FileUploadRequest
        {
            FileName = "malware.exe",
            ContentType = "application/octet-stream",
            FileSize = 1024
        };

        var result = _strategy.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Only PDF"));
    }

    /// <summary>
    /// Validates that Word documents (.docx) are rejected — only PDFs are allowed for signed agreements.
    /// </summary>
    [Fact]
    public void Validate_DocxFile_ReturnsInvalid()
    {
        var request = new FileUploadRequest
        {
            FileName = "document.docx",
            ContentType = "application/vnd.openxmlformats",
            FileSize = 1024
        };

        var result = _strategy.Validate(request);

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Validates that a null file name is rejected. Prevents NullReferenceException in downstream processing.
    /// </summary>
    [Fact]
    public void Validate_NullFileName_ReturnsInvalid()
    {
        var request = new FileUploadRequest
        {
            FileName = null!,
            ContentType = "application/pdf",
            FileSize = 1024
        };

        var result = _strategy.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("File name is required"));
    }

    /// <summary>
    /// Validates that an empty string file name is rejected (distinct from null).
    /// </summary>
    [Fact]
    public void Validate_EmptyFileName_ReturnsInvalid()
    {
        var request = new FileUploadRequest
        {
            FileName = "",
            ContentType = "application/pdf",
            FileSize = 1024
        };

        var result = _strategy.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("File name is required"));
    }

    /// <summary>
    /// Validates the maximum file size boundary: an 11 MB file exceeds the 10 MB limit.
    /// Prevents excessively large uploads from consuming server resources.
    /// </summary>
    [Fact]
    public void Validate_OversizedFile_ReturnsInvalid()
    {
        var request = new FileUploadRequest
        {
            FileName = "large.pdf",
            ContentType = "application/pdf",
            FileSize = 11 * 1024 * 1024
        };

        var result = _strategy.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("exceeds"));
    }

    /// <summary>
    /// Validates the minimum file size boundary: a 0-byte file is rejected as empty/corrupt.
    /// </summary>
    [Fact]
    public void Validate_ZeroSizeFile_ReturnsInvalid()
    {
        var request = new FileUploadRequest
        {
            FileName = "empty.pdf",
            ContentType = "application/pdf",
            FileSize = 0
        };

        var result = _strategy.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("empty"));
    }

    /// <summary>
    /// Validates that a file with a .pdf extension but a non-PDF content type (text/plain) is rejected.
    /// Catches attempts to bypass extension checks by manipulating the content type header.
    /// </summary>
    [Fact]
    public void Validate_PdfExtensionWrongContentType_ReturnsInvalid()
    {
        var request = new FileUploadRequest
        {
            FileName = "doc.pdf",
            ContentType = "text/plain",
            FileSize = 1024
        };

        var result = _strategy.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Content type must be application/pdf"));
    }

    /// <summary>
    /// Parameterized test covering multiple file type/size combinations in a single pass.
    /// Validates both the happy path (valid PDF at 5 MB) and rejection cases (shell scripts, images).
    /// </summary>
    [Theory]
    [InlineData("report.pdf", "application/pdf", 5 * 1024 * 1024, true)]
    [InlineData("script.sh", "application/x-sh", 512, false)]
    [InlineData("image.png", "image/png", 2048, false)]
    public void Validate_VariousFiles_ReturnsExpectedValidity(
        string fileName, string contentType, long size, bool expectedValid)
    {
        var request = new FileUploadRequest
        {
            FileName = fileName,
            ContentType = contentType,
            FileSize = size
        };

        var result = _strategy.Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }
}
