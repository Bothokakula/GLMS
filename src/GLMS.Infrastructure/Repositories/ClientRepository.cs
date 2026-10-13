// ==========================================================================
// File: ClientRepository.cs
// Purpose: Implements the IClientRepository interface using Entity Framework
//          Core, providing data access methods for Client entities including
//          retrieval (single and all) and creation.
// ==========================================================================

// Repository pattern implementation (Fowler, M., 2002, Patterns of Enterprise Application Architecture)
// Entity Framework Core data access (Microsoft, 2024, EF Core documentation)

using GLMS.Core.Interfaces;
using GLMS.Core.Models;
using GLMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Infrastructure.Repositories;

/// <summary>
/// Repository for <see cref="Client"/> entities, abstracting all EF Core
/// data access behind the <see cref="IClientRepository"/> interface so the
/// service layer remains persistence-ignorant.
/// </summary>
public class ClientRepository : IClientRepository
{
    private readonly GlmsDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="ClientRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core database context injected by the DI container.</param>
    public ClientRepository(GlmsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves a single client by primary key, eagerly loading its contracts.
    /// </summary>
    /// <param name="id">The client's primary key.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The matching <see cref="Client"/> or <c>null</c> if not found.</returns>
    public async Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Include(c => c.Contracts) eagerly loads the navigation property in a single
        // SQL query, avoiding the N+1 problem when the caller accesses contracts.
        return await _context.Clients
            .Include(c => c.Contracts)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves all clients, ordered alphabetically by name, with their contracts eagerly loaded.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>A read-only list of all <see cref="Client"/> entities.</returns>
    public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // OrderBy(c => c.Name) provides a deterministic, user-friendly sort order.
        // ToListAsync materializes the query into memory for the response.
        return await _context.Clients
            .Include(c => c.Contracts)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Persists a new client entity to the database.
    /// </summary>
    /// <param name="client">The client entity to add.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The same <see cref="Client"/> instance with its database-generated ID populated.</returns>
    public async Task<Client> AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        // EF Core tracks the new entity; SaveChangesAsync sends the INSERT and
        // populates the auto-generated primary key on the entity instance.
        _context.Clients.Add(client);
        await _context.SaveChangesAsync(cancellationToken);
        return client;
    }
}
