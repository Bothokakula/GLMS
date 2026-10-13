// File: Services/Validation/ContractStatusValidationStrategy.cs
// Purpose: Concrete Strategy that validates contract status transitions against
//          a whitelist of allowed state-machine edges. By encoding the transition
//          rules in a declarative dictionary, the logic is easy to audit and extend.
// Implementation of the Strategy pattern (Gamma et al., 1994)
// Reference: Gamma, E., Helm, R., Johnson, R. and Vlissides, J. (1994)
//            Design Patterns: Elements of Reusable Object-Oriented Software.

using GLMS.Core.DTOs;
using GLMS.Core.Interfaces;
using GLMS.Core.Models;

namespace GLMS.Core.Services.Validation;

/// <summary>
/// Validates that a requested contract status transition is permitted by the
/// system's state-machine rules. Implements <see cref="IValidationStrategy{T}"/>
/// for <see cref="StatusChangeRequest"/>.
/// <para>
/// Following the Strategy pattern (Gamma et al., 1994), this class encapsulates
/// the transition-validation algorithm so it can be swapped or extended without
/// modifying the <see cref="ContractService"/> that consumes it.
/// </para>
/// </summary>
public class ContractStatusValidationStrategy : IValidationStrategy<StatusChangeRequest>
{
    /// <summary>
    /// Declarative map of every <see cref="ContractStatus"/> to the statuses it
    /// may transition to. Acts as the single source of truth for the contract
    /// lifecycle state machine:
    ///   Draft  → Active
    ///   Active → Expired | OnHold
    ///   OnHold → Active
    ///   Expired → (terminal — no outgoing transitions)
    /// </summary>
    private static readonly Dictionary<ContractStatus, ContractStatus[]> AllowedTransitions = new()
    {
        { ContractStatus.Draft, new[] { ContractStatus.Active } },
        { ContractStatus.Active, new[] { ContractStatus.Expired, ContractStatus.OnHold } },
        { ContractStatus.OnHold, new[] { ContractStatus.Active } },
        { ContractStatus.Expired, Array.Empty<ContractStatus>() }
    };

    /// <inheritdoc />
    /// <remarks>
    /// Checks the <see cref="AllowedTransitions"/> dictionary to determine whether
    /// the requested transition is valid. Returns all applicable errors so the caller
    /// can present them to the user in a single response.
    /// </remarks>
    public ValidationResult Validate(StatusChangeRequest entity)
    {
        var result = new ValidationResult();

        if (!AllowedTransitions.TryGetValue(entity.CurrentStatus, out var allowed))
        {
            result.AddError($"Unknown contract status: {entity.CurrentStatus}.");
            return result;
        }

        if (!allowed.Contains(entity.NewStatus))
        {
            result.AddError(
                $"Cannot transition from {entity.CurrentStatus} to {entity.NewStatus}. " +
                $"Allowed transitions: {(allowed.Length > 0 ? string.Join(", ", allowed) : "none")}.");
        }

        return result;
    }
}
