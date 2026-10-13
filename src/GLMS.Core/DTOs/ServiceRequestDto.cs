// File: DTOs/ServiceRequestDto.cs
// Purpose: Read-only projection of a ServiceRequest entity for API responses.
//          Flattens the Contract navigation property into ContractTitle so the
//          client receives a self-contained payload without needing a second call.

namespace GLMS.Core.DTOs;

/// <summary>
/// Data Transfer Object returned by API endpoints when listing or retrieving
/// service requests. Contains pre-formatted fields suitable for direct
/// rendering in a UI or report.
/// </summary>
public class ServiceRequestDto
{
    /// <summary>Unique identifier of the service request.</summary>
    public int Id { get; set; }

    /// <summary>Identifier of the parent contract.</summary>
    public int ContractId { get; set; }

    /// <summary>
    /// Title of the parent contract, included here to avoid an extra lookup
    /// when displaying service request lists.
    /// </summary>
    public string ContractTitle { get; set; } = string.Empty;

    /// <summary>Description of the work requested.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Cost in US Dollars (source currency).</summary>
    public decimal CostUSD { get; set; }

    /// <summary>Cost in South African Rand (converted currency).</summary>
    public decimal CostZAR { get; set; }

    /// <summary>USD→ZAR exchange rate captured at creation time.</summary>
    public decimal ExchangeRate { get; set; }

    /// <summary>Current workflow status (e.g. "Pending", "Active").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the service request was created.</summary>
    public DateTime CreatedDate { get; set; }
}
