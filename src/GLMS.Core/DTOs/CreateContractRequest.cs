// File: DTOs/CreateContractRequest.cs
// Purpose: Inbound DTO for creating a new contract. Consumed by ContractService,
//          which delegates actual object construction to ContractFactory (Factory
//          Method pattern) so creation logic remains in a single place.
// Reference: Factory Method pattern — Gamma, E., Helm, R., Johnson, R. and
//            Vlissides, J. (1994) Design Patterns: Elements of Reusable
//            Object-Oriented Software.

using GLMS.Core.Models;

namespace GLMS.Core.DTOs;

/// <summary>
/// Request payload for creating a new <see cref="Contract"/>. The resulting
/// contract is always initialised in <see cref="ContractStatus.Draft"/> status
/// by the <see cref="Interfaces.IContractFactory"/>.
/// </summary>
public class CreateContractRequest
{
    /// <summary>
    /// Identifier of the client who will own the new contract.
    /// </summary>
    public int ClientId { get; set; }

    /// <summary>
    /// Human-readable title for the contract.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Requested start date for the contract.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Requested end date. If invalid or earlier than <see cref="StartDate"/>,
    /// the <see cref="Services.ContractFactory"/> will substitute a default
    /// duration based on the chosen <see cref="ServiceLevel"/>.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Desired service tier (Standard or Premium).
    /// </summary>
    public ServiceLevel ServiceLevel { get; set; }
}
