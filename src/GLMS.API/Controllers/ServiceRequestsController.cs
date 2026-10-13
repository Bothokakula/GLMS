// ==========================================================================
// File: ServiceRequestsController.cs
// Purpose: Exposes RESTful endpoints for managing service requests that are
//          linked to contracts. Service requests represent individual work
//          items (e.g., a shipment) with costs tracked in both USD and ZAR.
// ==========================================================================

// RESTful API design (Fielding, R., 2000, Architectural Styles and the Design of Network-based Software Architectures)

using GLMS.Core.DTOs;
using GLMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.API.Controllers;

/// <summary>
/// Manages service request resources that belong to a parent contract.
/// All endpoints require JWT authentication.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestService _serviceRequestService;
    private readonly ICurrencyService _currencyService;

    /// <summary>
    /// Initializes a new instance of <see cref="ServiceRequestsController"/>.
    /// </summary>
    /// <param name="serviceRequestService">Domain service encapsulating service-request business logic.</param>
    /// <param name="currencyService">Currency conversion service (injected for potential in-controller usage).</param>
    public ServiceRequestsController(
        IServiceRequestService serviceRequestService,
        ICurrencyService currencyService)
    {
        _serviceRequestService = serviceRequestService;
        _currencyService = currencyService;
    }

    /// <summary>
    /// Retrieves all service requests belonging to a specific contract.
    /// </summary>
    /// <param name="contractId">The parent contract's primary key, passed as a query parameter.</param>
    /// <returns>
    /// 200 OK with a list of <see cref="ServiceRequestDto"/> objects.
    /// </returns>
    /// <remarks>
    /// GET api/servicerequests?contractId=1
    /// Uses a query parameter rather than a route segment because service requests
    /// are a top-level resource filtered by contract, not a nested sub-resource.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetByContract([FromQuery] int contractId)
    {
        var serviceRequests = await _serviceRequestService.GetByContractIdAsync(contractId);

        // Map domain entities to DTOs, including the parent contract title for display convenience.
        var dtos = serviceRequests.Select(sr => new ServiceRequestDto
        {
            Id = sr.Id,
            ContractId = sr.ContractId,
            ContractTitle = sr.Contract?.Title ?? string.Empty,
            Description = sr.Description,
            CostUSD = sr.CostUSD,
            CostZAR = sr.CostZAR,
            ExchangeRate = sr.ExchangeRate,
            Status = sr.Status,
            CreatedDate = sr.CreatedDate
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Creates a new service request under an existing contract.
    /// </summary>
    /// <param name="request">Service request details including contract ID, description, and cost.</param>
    /// <returns>
    /// 201 Created with the new <see cref="ServiceRequestDto"/> and a Location header;
    /// 404 Not Found if the referenced contract does not exist;
    /// 409 Conflict if business rules prevent creation (e.g., contract is not active).
    /// </returns>
    /// <remarks>
    /// POST api/servicerequests
    /// Delegates to the service layer which handles currency conversion and
    /// observer notifications (e.g., audit logging).
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequestRequest request)
    {
        try
        {
            var serviceRequest = await _serviceRequestService.CreateAsync(request);

            var dto = new ServiceRequestDto
            {
                Id = serviceRequest.Id,
                ContractId = serviceRequest.ContractId,
                ContractTitle = serviceRequest.Contract?.Title ?? string.Empty,
                Description = serviceRequest.Description,
                CostUSD = serviceRequest.CostUSD,
                CostZAR = serviceRequest.CostZAR,
                ExchangeRate = serviceRequest.ExchangeRate,
                Status = serviceRequest.Status,
                CreatedDate = serviceRequest.CreatedDate
            };

            return CreatedAtAction(nameof(GetByContract), new { contractId = dto.ContractId }, dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}
