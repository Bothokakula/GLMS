// ==========================================================================
// File: ContractsController.cs
// Purpose: Exposes RESTful endpoints for the full contract lifecycle —
//          listing (with optional filters), retrieving by ID, creating,
//          updating status, and uploading/downloading signed agreement files.
// ==========================================================================

// RESTful API design (Fielding, R., 2000, Architectural Styles and the Design of Network-based Software Architectures)

using GLMS.Core.DTOs;
using GLMS.Core.Interfaces;
using GLMS.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.API.Controllers;

/// <summary>
/// Request body for the status-update endpoint. Uses a C# record for concise,
/// immutable data transfer.
/// </summary>
/// <param name="NewStatus">The target <see cref="ContractStatus"/> to transition to.</param>
public record StatusUpdateBody(ContractStatus NewStatus);

/// <summary>
/// Manages contract resources including CRUD operations, status transitions,
/// and file upload/download for signed agreements. All endpoints require
/// JWT authentication.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContractsController : ControllerBase
{
    private readonly IContractService _contractService;
    private readonly IFileService _fileService;

    /// <summary>
    /// Initializes a new instance of <see cref="ContractsController"/>.
    /// </summary>
    /// <param name="contractService">Domain service encapsulating contract business logic.</param>
    /// <param name="fileService">File storage service for managing signed agreement documents.</param>
    public ContractsController(IContractService contractService, IFileService fileService)
    {
        _contractService = contractService;
        _fileService = fileService;
    }

    /// <summary>
    /// Retrieves all contracts, optionally filtered by status, date range, or client name.
    /// </summary>
    /// <param name="status">Filter by contract status (e.g., Active, Draft, Expired).</param>
    /// <param name="startDate">Include contracts starting on or after this date.</param>
    /// <param name="endDate">Include contracts ending on or before this date.</param>
    /// <param name="clientName">Partial match on the associated client's name.</param>
    /// <returns>
    /// 200 OK with a list of <see cref="ContractDto"/> objects matching the filters.
    /// </returns>
    /// <remarks>
    /// GET api/contracts?status=Active&amp;startDate=2026-01-01&amp;endDate=2027-12-31&amp;clientName=Maersk
    /// All query parameters are optional; omitting them returns the full list.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] ContractStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? clientName)
    {
        var filter = new ContractFilter
        {
            Status = status,
            StartDate = startDate,
            EndDate = endDate,
            ClientName = clientName
        };

        var contracts = await _contractService.GetAllAsync(filter);

        // Project domain entities to DTOs, converting enums to strings for JSON readability.
        var dtos = contracts.Select(c => new ContractDto
        {
            Id = c.Id,
            ClientId = c.ClientId,
            ClientName = c.Client?.Name ?? string.Empty,
            Title = c.Title,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            Status = c.Status.ToString(),
            ServiceLevel = c.ServiceLevel.ToString(),
            SignedAgreementPath = c.SignedAgreementPath,
            OriginalFileName = c.OriginalFileName
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Retrieves a single contract by its unique identifier.
    /// </summary>
    /// <param name="id">The contract's primary key.</param>
    /// <returns>
    /// 200 OK with a <see cref="ContractDto"/> if found;
    /// 404 Not Found if no contract exists with the given ID.
    /// </returns>
    /// <remarks>
    /// GET api/contracts/{id}
    /// </remarks>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract is null)
            return NotFound();

        var dto = new ContractDto
        {
            Id = contract.Id,
            ClientId = contract.ClientId,
            ClientName = contract.Client?.Name ?? string.Empty,
            Title = contract.Title,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            Status = contract.Status.ToString(),
            ServiceLevel = contract.ServiceLevel.ToString(),
            SignedAgreementPath = contract.SignedAgreementPath,
            OriginalFileName = contract.OriginalFileName
        };

        return Ok(dto);
    }

    /// <summary>
    /// Creates a new contract linked to an existing client.
    /// </summary>
    /// <param name="request">Contract details including client ID, title, dates, and service level.</param>
    /// <returns>
    /// 201 Created with the new <see cref="ContractDto"/> and a Location header;
    /// 404 Not Found if the referenced client does not exist.
    /// </returns>
    /// <remarks>
    /// POST api/contracts
    /// Delegates to the service layer which uses the Factory pattern to construct
    /// the contract entity with appropriate defaults.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContractRequest request)
    {
        try
        {
            var contract = await _contractService.CreateContractAsync(request);

            var dto = new ContractDto
            {
                Id = contract.Id,
                ClientId = contract.ClientId,
                ClientName = contract.Client?.Name ?? string.Empty,
                Title = contract.Title,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Status = contract.Status.ToString(),
                ServiceLevel = contract.ServiceLevel.ToString()
            };

            return CreatedAtAction(nameof(GetById), new { id = contract.Id }, dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Transitions a contract to a new status (e.g., Draft → Active → Expired).
    /// </summary>
    /// <param name="id">The contract's primary key.</param>
    /// <param name="body">Request body containing the target status.</param>
    /// <returns>
    /// 200 OK on successful transition;
    /// 404 Not Found if the contract does not exist;
    /// 400 Bad Request if the transition violates business rules (e.g., invalid state change).
    /// </returns>
    /// <remarks>
    /// PATCH api/contracts/{id}/status
    /// Uses PATCH rather than PUT because only a single field is being modified.
    /// The service layer validates allowed state transitions before persisting.
    /// </remarks>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusUpdateBody body)
    {
        try
        {
            await _contractService.UpdateStatusAsync(id, body.NewStatus);
            return Ok(new { message = "Status updated successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Uploads a signed agreement PDF (or other document) for a contract.
    /// </summary>
    /// <param name="id">The contract's primary key.</param>
    /// <param name="file">The uploaded file from the multipart form data.</param>
    /// <returns>
    /// 200 OK with the saved file path on success;
    /// 400 Bad Request if no file is provided or validation fails;
    /// 404 Not Found if the contract does not exist.
    /// </returns>
    /// <remarks>
    /// POST api/contracts/{id}/upload
    /// The file is validated (type, size) via the Strategy pattern before being
    /// persisted to the local file system under the /uploads directory.
    /// </remarks>
    [HttpPost("{id}/upload")]
    public async Task<IActionResult> UploadAgreement(int id, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var contract = await _contractService.GetByIdAsync(id);
        if (contract is null)
            return NotFound();

        // Delegate validation to the file validation strategy, keeping
        // the controller free of format/size-checking logic.
        var validation = _fileService.ValidateFile(file.FileName, file.ContentType, file.Length);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });

        try
        {
            await using var stream = file.OpenReadStream();
            var savedPath = await _fileService.SaveFileAsync(stream, file.FileName);

            contract.SignedAgreementPath = savedPath;
            contract.OriginalFileName = file.FileName;
            await _contractService.UpdateContractAsync(contract);

            return Ok(new { message = "Agreement uploaded successfully.", path = savedPath });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Downloads the signed agreement document for a contract.
    /// </summary>
    /// <param name="id">The contract's primary key.</param>
    /// <returns>
    /// A file stream with the appropriate content type and original filename;
    /// 404 Not Found if the contract does not exist or no agreement has been uploaded.
    /// </returns>
    /// <remarks>
    /// GET api/contracts/{id}/download
    /// Returns the file with its original filename so the browser can prompt a
    /// meaningful "Save As" dialog.
    /// </remarks>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadAgreement(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract is null)
            return NotFound();

        if (string.IsNullOrEmpty(contract.SignedAgreementPath))
            return NotFound(new { error = "No agreement file uploaded for this contract." });

        var fileResult = await _fileService.GetFileAsync(contract.SignedAgreementPath);
        if (fileResult is null)
            return NotFound(new { error = "Agreement file not found on disk." });

        var (stream, contentType) = fileResult.Value;
        var fileName = contract.OriginalFileName ?? "agreement.pdf";
        return File(stream, contentType, fileName);
    }
}
