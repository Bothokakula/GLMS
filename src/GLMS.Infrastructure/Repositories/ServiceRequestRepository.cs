// ==========================================================================
// File: ServiceRequestRepository.cs
// Purpose: Implements the IServiceRequestRepository interface using Entity
//          Framework Core, providing data access for ServiceRequest entities
//          including retrieval by ID, retrieval by parent contract, creation,
//          and batch updates.
// ==========================================================================

// Repository pattern implementation (Fowler, M., 2002, Patterns of Enterprise Application Architecture)
// Entity Framework Core data access (Microsoft, 2024, EF Core documentation)

using GLMS.Core.Interfaces;
using GLMS.Core.Models;
using GLMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Infrastructure.Repositories;

/// <summary>
/// Repository for <see cref="ServiceRequest"/> entities, abstracting EF Core
/// data access behind the <see cref="IServiceRequestRepository"/> interface
/// to support the service layer and enable testability via mocking.
/// </summary>
public class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly GlmsDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="ServiceRequestRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core database context injected by the DI container.</param>
    public ServiceRequestRepository(GlmsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves a single service request by primary key, eagerly loading the parent
    /// contract and its associated client for display purposes.
    /// </summary>
    /// <param name="id">The service request's primary key.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The matching <see cref="ServiceRequest"/> or <c>null</c> if not found.</returns>
    public async Task<ServiceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // ThenInclude navigates from ServiceRequest → Contract → Client in a single
        // query, providing the full object graph without lazy-loading proxies.
        return await _context.ServiceRequests
            .Include(sr => sr.Contract)
                .ThenInclude(c => c!.Client)
            .FirstOrDefaultAsync(sr => sr.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves all service requests belonging to a specific contract, ordered newest first.
    /// </summary>
    /// <param name="contractId">The parent contract's primary key used to filter results.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>A read-only list of <see cref="ServiceRequest"/> entities for the given contract.</returns>
    public async Task<IReadOnlyList<ServiceRequest>> GetByContractIdAsync(int contractId, CancellationToken cancellationToken = default)
    {
        // Where filters to the specified contract; OrderByDescending ensures the most
        // recent requests appear first, matching the expected UI display order.
        return await _context.ServiceRequests
            .Where(sr => sr.ContractId == contractId)
            .Include(sr => sr.Contract)
            .OrderByDescending(sr => sr.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Persists a new service request entity to the database.
    /// </summary>
    /// <param name="serviceRequest">The service request entity to add.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The same <see cref="ServiceRequest"/> instance with its database-generated ID populated.</returns>
    public async Task<ServiceRequest> AddAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default)
    {
        _context.ServiceRequests.Add(serviceRequest);
        await _context.SaveChangesAsync(cancellationToken);
        return serviceRequest;
    }

    /// <summary>
    /// Batch-updates multiple service request entities in a single database round-trip.
    /// Used when a contract status change cascades status updates to all its service requests.
    /// </summary>
    /// <param name="serviceRequests">The collection of modified service request entities.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    public async Task UpdateRangeAsync(IEnumerable<ServiceRequest> serviceRequests, CancellationToken cancellationToken = default)
    {
        // UpdateRange marks all entities as modified; EF Core generates a single
        // transaction with individual UPDATE statements, ensuring atomicity.
        _context.ServiceRequests.UpdateRange(serviceRequests);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
