// File: Services/Observers/ServiceRequestStatusObserver.cs
// Purpose: Concrete Observer that cascades contract status changes down to child
//          service requests. When a contract moves to Expired or OnHold, all of
//          its Pending/Active service requests are bulk-updated to "Cancelled" or
//          "Suspended" respectively, ensuring data consistency across the aggregate.
// Implementation of the Observer pattern (Gamma et al., 1994)
// Reference: Gamma, E., Helm, R., Johnson, R. and Vlissides, J. (1994)
//            Design Patterns: Elements of Reusable Object-Oriented Software.

using GLMS.Core.Interfaces;
using GLMS.Core.Models;

namespace GLMS.Core.Services.Observers;

/// <summary>
/// Observer that reacts to contract status transitions by updating the status
/// of associated <see cref="ServiceRequest"/> entities. Registered as an
/// <see cref="IContractObserver"/> in the DI container and invoked by
/// <see cref="ContractService.UpdateStatusAsync"/> after a successful transition.
/// <para>
/// This follows the Observer pattern (Gamma et al., 1994), keeping the cascade
/// logic decoupled from the <see cref="ContractService"/> so it can be added,
/// removed, or replaced without modifying the subject.
/// </para>
/// </summary>
public class ServiceRequestStatusObserver : IContractObserver
{
    private readonly IServiceRequestRepository _serviceRequestRepository;

    /// <summary>
    /// Initialises a new instance of <see cref="ServiceRequestStatusObserver"/>.
    /// </summary>
    /// <param name="serviceRequestRepository">
    /// Repository used to fetch and batch-update child service requests.
    /// </param>
    public ServiceRequestStatusObserver(IServiceRequestRepository serviceRequestRepository)
    {
        _serviceRequestRepository = serviceRequestRepository;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Only acts when the new status is <see cref="ContractStatus.Expired"/> or
    /// <see cref="ContractStatus.OnHold"/>:
    /// <list type="bullet">
    ///   <item><description>Expired → all Pending/Active requests become "Cancelled".</description></item>
    ///   <item><description>OnHold  → all Pending/Active requests become "Suspended".</description></item>
    /// </list>
    /// The bulk update is skipped entirely if no requests match, avoiding unnecessary
    /// database round-trips.
    /// </remarks>
    public async Task OnStatusChanged(int contractId, ContractStatus oldStatus, ContractStatus newStatus)
    {
        // Only Expired and OnHold transitions require cascading updates to child requests.
        if (newStatus is not (ContractStatus.Expired or ContractStatus.OnHold))
            return;

        var targetStatus = newStatus == ContractStatus.Expired ? "Cancelled" : "Suspended";
        var serviceRequests = await _serviceRequestRepository.GetByContractIdAsync(contractId);

        var toUpdate = serviceRequests.Where(sr => sr.Status is "Pending" or "Active").ToList();
        foreach (var request in toUpdate)
        {
            request.Status = targetStatus;
        }

        if (toUpdate.Count > 0)
        {
            await _serviceRequestRepository.UpdateRangeAsync(toUpdate);
        }
    }
}
