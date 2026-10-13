// File: DTOs/CreateServiceRequestRequest.cs
// Purpose: Inbound DTO for creating a new service request against an existing contract.
//          The caller supplies the cost in USD; ZAR conversion happens server-side
//          using the live exchange rate from CurrencyService.

namespace GLMS.Core.DTOs;

/// <summary>
/// Request payload for creating a new <see cref="Models.ServiceRequest"/>.
/// The ZAR cost and exchange rate are not supplied by the caller — they are
/// computed automatically by <see cref="Interfaces.ICurrencyService"/>.
/// </summary>
public class CreateServiceRequestRequest
{
    /// <summary>
    /// Identifier of the contract to which this service request belongs.
    /// The contract must be in <see cref="Models.ContractStatus.Active"/> status.
    /// </summary>
    public int ContractId { get; set; }

    /// <summary>
    /// Free-text description of the work being requested.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Estimated or quoted cost in US Dollars. Will be converted to ZAR
    /// at the prevailing exchange rate and stored alongside the original amount.
    /// </summary>
    public decimal CostUSD { get; set; }
}
