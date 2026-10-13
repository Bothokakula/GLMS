// File: DTOs/ContractDto.cs
// Purpose: Read-only projection of a Contract entity returned by API endpoints.
//          Flattens the Client navigation into ClientName and converts enums to
//          strings so the payload is immediately renderable by front-end clients.

namespace GLMS.Core.DTOs;

/// <summary>
/// Data Transfer Object for contract data exposed through the API layer.
/// Enum values (<see cref="Models.ContractStatus"/>, <see cref="Models.ServiceLevel"/>)
/// are serialised as strings for readability.
/// </summary>
public class ContractDto
{
    /// <summary>Unique identifier of the contract.</summary>
    public int Id { get; set; }

    /// <summary>Identifier of the owning client.</summary>
    public int ClientId { get; set; }

    /// <summary>
    /// Name of the owning client, denormalised here to avoid an extra API call
    /// when rendering contract lists.
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>Human-readable title of the contract.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Date the contract becomes effective.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Date the contract expires.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Current lifecycle status as a string (e.g. "Active", "Draft").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Service tier as a string (e.g. "Standard", "Premium").</summary>
    public string ServiceLevel { get; set; } = string.Empty;

    /// <summary>Server-side path to the signed agreement PDF, if uploaded.</summary>
    public string? SignedAgreementPath { get; set; }

    /// <summary>Original file name of the uploaded agreement, if any.</summary>
    public string? OriginalFileName { get; set; }
}
