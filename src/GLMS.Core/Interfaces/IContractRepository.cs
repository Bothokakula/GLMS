// File: Interfaces/IContractRepository.cs
// Purpose: Repository abstraction for Contract persistence. Decouples the service
//          layer from the ORM so the data-access implementation can be swapped
//          (e.g. from EF Core to Dapper) without changing business logic.
// Reference: Asynchronous programming with async and await —
//            Microsoft (2024) Asynchronous programming with async and await.

using GLMS.Core.DTOs;
using GLMS.Core.Models;

namespace GLMS.Core.Interfaces;

/// <summary>
/// Data-access contract for <see cref="Contract"/> entities.
/// Supports CRUD operations and filtered queries with optional cancellation.
/// </summary>
public interface IContractRepository
{
    /// <summary>
    /// Retrieves a single contract by its primary key, including related entities.
    /// </summary>
    /// <param name="id">The contract identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The matching contract, or <c>null</c> if not found.</returns>
    Task<Contract?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all contracts matching the supplied filter criteria.
    /// </summary>
    /// <param name="filter">Optional filters to narrow the result set.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of matching contracts.</returns>
    Task<IReadOnlyList<Contract>> GetAllAsync(ContractFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new contract to the data store.
    /// </summary>
    /// <param name="contract">The entity to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The persisted entity with a database-generated Id.</returns>
    Task<Contract> AddAsync(Contract contract, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing contract in the data store.
    /// </summary>
    /// <param name="contract">The modified entity to save.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task UpdateAsync(Contract contract, CancellationToken cancellationToken = default);
}
