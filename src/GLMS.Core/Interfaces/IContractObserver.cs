// File: Interfaces/IContractObserver.cs
// Purpose: Defines the observer interface for the Observer pattern applied to
//          contract status changes. Concrete observers react to status transitions
//          without the ContractService needing to know about them at compile time.
// Implementation of the Observer pattern (Gamma et al., 1994)
// Reference: Gamma, E., Helm, R., Johnson, R. and Vlissides, J. (1994)
//            Design Patterns: Elements of Reusable Object-Oriented Software.

using GLMS.Core.Models;

namespace GLMS.Core.Interfaces;

/// <summary>
/// Observer interface notified whenever a contract's lifecycle status changes.
/// Multiple observers can be registered via dependency injection; the
/// <see cref="Services.ContractService"/> iterates over all of them after
/// a successful status transition.
/// <para>
/// This follows the Observer pattern (Gamma et al., 1994), decoupling the
/// subject (ContractService) from the concrete side-effects (e.g. cascading
/// service-request status updates, audit logging).
/// </para>
/// </summary>
public interface IContractObserver
{
    /// <summary>
    /// Invoked after a contract's status has been persisted, giving the
    /// observer a chance to react (e.g. update child entities, write logs).
    /// </summary>
    /// <param name="contractId">The identifier of the contract whose status changed.</param>
    /// <param name="oldStatus">The status before the transition.</param>
    /// <param name="newStatus">The status after the transition.</param>
    Task OnStatusChanged(int contractId, ContractStatus oldStatus, ContractStatus newStatus);
}
