// File: Interfaces/IClientRepository.cs
// Purpose: Repository abstraction for Client persistence. Keeps the service layer
//          independent of the data-access technology used in the infrastructure layer.
// Reference: Asynchronous programming with async and await —
//            Microsoft (2024) Asynchronous programming with async and await.

using GLMS.Core.Models;

namespace GLMS.Core.Interfaces;

/// <summary>
/// Data-access contract for <see cref="Client"/> entities.
/// All methods accept an optional <see cref="CancellationToken"/> to support
/// cooperative cancellation of database operations.
/// </summary>
public interface IClientRepository
{
    /// <summary>
    /// Retrieves a single client by its primary key.
    /// </summary>
    /// <param name="id">The client identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The matching client, or <c>null</c> if not found.</returns>
    Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all clients in the system.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of all clients.</returns>
    Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new client to the data store.
    /// </summary>
    /// <param name="client">The entity to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The persisted entity with a database-generated Id.</returns>
    Task<Client> AddAsync(Client client, CancellationToken cancellationToken = default);
}
