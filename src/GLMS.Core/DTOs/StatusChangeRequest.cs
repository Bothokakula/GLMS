// File: DTOs/StatusChangeRequest.cs
// Purpose: Data Transfer Object that captures the intent to transition a contract
//          from one lifecycle status to another. Consumed by the Strategy pattern
//          validation (ContractStatusValidationStrategy) to determine whether the
//          requested transition is permitted.
// Reference: Strategy pattern — Gamma, E., Helm, R., Johnson, R. and Vlissides, J.
//            (1994) Design Patterns: Elements of Reusable Object-Oriented Software.

using GLMS.Core.Models;

namespace GLMS.Core.DTOs;

/// <summary>
/// Represents a request to change a contract's status from
/// <see cref="CurrentStatus"/> to <see cref="NewStatus"/>. Passed to
/// <see cref="Interfaces.IValidationStrategy{T}"/> so the strategy can
/// evaluate whether the transition is allowed without coupling to the
/// contract entity itself.
/// </summary>
public class StatusChangeRequest
{
    /// <summary>
    /// Identifier of the contract whose status is being changed.
    /// </summary>
    public int ContractId { get; set; }

    /// <summary>
    /// The contract's current lifecycle status before the transition.
    /// </summary>
    public ContractStatus CurrentStatus { get; set; }

    /// <summary>
    /// The desired lifecycle status after the transition.
    /// </summary>
    public ContractStatus NewStatus { get; set; }
}
