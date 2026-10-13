// ==========================================================================
// File: ContractRepository.cs
// Purpose: Implements the IContractRepository interface using Entity Framework
//          Core, providing data access for Contract entities with support for
//          dynamic filtering, creation, and updates.
// ==========================================================================

// Repository pattern implementation (Fowler, M., 2002, Patterns of Enterprise Application Architecture)
// Entity Framework Core data access (Microsoft, 2024, EF Core documentation)

using GLMS.Core.DTOs;
using GLMS.Core.Interfaces;
using GLMS.Core.Models;
using GLMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Infrastructure.Repositories;

/// <summary>
/// Repository for <see cref="Contract"/> entities, encapsulating query logic
/// (including dynamic filtering) behind the <see cref="IContractRepository"/> interface.
/// </summary>
public class ContractRepository : IContractRepository
{
    private readonly GlmsDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="ContractRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core database context injected by the DI container.</param>
    public ContractRepository(GlmsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves a single contract by primary key, eagerly loading the parent client
    /// and child service requests.
    /// </summary>
    /// <param name="id">The contract's primary key.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The matching <see cref="Contract"/> or <c>null</c> if not found.</returns>
    public async Task<Contract?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Two Include calls eagerly load both the Client and ServiceRequests navigation
        // properties so the caller has the full aggregate without additional queries.
        return await _context.Contracts
            .Include(c => c.Client)
            .Include(c => c.ServiceRequests)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves all contracts matching the supplied filter criteria, ordered by most recent start date.
    /// </summary>
    /// <param name="filter">
    /// Optional filter criteria — status, date range, and/or client name.
    /// Each non-null filter property adds a WHERE clause to the underlying LINQ query.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>A read-only list of <see cref="Contract"/> entities matching the filter.</returns>
    public async Task<IReadOnlyList<Contract>> GetAllAsync(ContractFilter filter, CancellationToken cancellationToken = default)
    {
        // Start with a base query that includes the Client navigation property
        // for display purposes (e.g., showing client name alongside contract data).
        IQueryable<Contract> query = _context.Contracts.Include(c => c.Client);

        // Conditionally compose WHERE clauses — only active filters modify the query.
        // This approach avoids branching into separate query paths and lets EF Core
        // translate the composed expression tree into a single efficient SQL statement.

        if (filter.Status.HasValue)
        {
            query = query.Where(c => c.Status == filter.Status.Value);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(c => c.StartDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(c => c.EndDate <= filter.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ClientName))
        {
            // Case-insensitive partial match is handled by SQL Server's default collation.
            var clientName = filter.ClientName.Trim();
            query = query.Where(c => c.Client != null && c.Client.Name.Contains(clientName));
        }

        // Order by StartDate descending so the most recent contracts appear first.
        return await query
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Persists a new contract entity to the database.
    /// </summary>
    /// <param name="contract">The contract entity to add.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The same <see cref="Contract"/> instance with its database-generated ID populated.</returns>
    public async Task<Contract> AddAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync(cancellationToken);
        return contract;
    }

    /// <summary>
    /// Saves changes to an existing contract entity (e.g., status update, file path).
    /// </summary>
    /// <param name="contract">The modified contract entity.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    public async Task UpdateAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        // Update marks all properties as modified; EF Core generates an UPDATE
        // statement covering every column — acceptable for low-frequency writes.
        _context.Contracts.Update(contract);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
