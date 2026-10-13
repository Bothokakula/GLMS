// File: Interfaces/IServiceRequestRepository.cs
// Purpose: Repository abstraction for ServiceRequest persistence. Follows the
//          Repository pattern to decouple the domain/service layer from the
//          concrete data-access technology (e.g. Entity Framework Core).
// Reference: Asynchronous programming with async and await —
//            Microsoft (2024) Asynchronous programming with async and await.

using GLMS.Core.Models;

namespace GLMS.Core.Interfaces;

/// <summary>
/// Data-access contract for <see cref="ServiceRequest"/> entities.
/// All methods accept an optional <see cref="CancellationToken"/> to support
/// graceful cancellation of long-running database operations.
/// </summary>
public interface IServiceRequestRepository
{
    /// <summary>
    /// Retrieves a single service request by its primary key.
    /// </summary>
    /// <param name="id">The service request identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The matching <see cref="ServiceRequest"/>, or <c>null</c> if not found.</returns>
    Task<ServiceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all service requests associated with a given contract.
    /// </summary>
    /// <param name="contractId">The parent contract identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of matching service requests.</returns>
    Task<IReadOnlyList<ServiceRequest>> GetByContractIdAsync(int contractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new service request to the data store.
    /// </summary>
    /// <param name="serviceRequest">The entity to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The persisted entity, now populated with a database-generated Id.</returns>
    Task<ServiceRequest> AddAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple service requests in a single batch. Used by observers
    /// (e.g. <see cref="Services.Observers.ServiceRequestStatusObserver"/>)
    /// to cascade status changes efficiently.
    /// </summary>
    /// <param name="serviceRequests">The entities to update.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task UpdateRangeAsync(IEnumerable<ServiceRequest> serviceRequests, CancellationToken cancellationToken = default);
}
