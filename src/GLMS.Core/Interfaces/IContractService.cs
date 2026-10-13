// File: Interfaces/IContractService.cs
// Purpose: Application-level service contract for contract CRUD and lifecycle
//          management. Orchestrates the Factory Method (creation), Strategy
//          (validation), and Observer (status-change notifications) patterns.
// References:
//   - Factory Method, Strategy, and Observer patterns — Gamma, E., Helm, R.,
//     Johnson, R. and Vlissides, J. (1994) Design Patterns: Elements of Reusable
//     Object-Oriented Software.
//   - Asynchronous programming with async and await —
//     Microsoft (2024) Asynchronous programming with async and await.

using GLMS.Core.DTOs;
using GLMS.Core.Models;

namespace GLMS.Core.Interfaces;

/// <summary>
/// Service contract for managing the full lifecycle of <see cref="Contract"/>
/// entities — creation, retrieval, status transitions, and general updates.
/// </summary>
public interface IContractService
{
    /// <summary>
    /// Creates a new contract using the Factory Method pattern and persists it.
    /// </summary>
    /// <param name="request">The contract creation parameters.</param>
    /// <returns>The persisted contract with a database-generated Id.</returns>
    Task<Contract> CreateContractAsync(CreateContractRequest request);

    /// <summary>
    /// Retrieves a single contract by its primary key, including navigation properties.
    /// </summary>
    /// <param name="id">The contract identifier.</param>
    /// <returns>The matching contract, or <c>null</c> if not found.</returns>
    Task<Contract?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves all contracts matching the specified filter criteria.
    /// </summary>
    /// <param name="filter">Optional filter parameters (status, dates, client name).</param>
    /// <returns>A read-only list of matching contracts.</returns>
    Task<IReadOnlyList<Contract>> GetAllAsync(ContractFilter filter);

    /// <summary>
    /// Transitions a contract to a new status after validating the transition
    /// via the Strategy pattern, then notifies all registered observers.
    /// </summary>
    /// <param name="contractId">The contract to update.</param>
    /// <param name="newStatus">The desired new status.</param>
    /// <exception cref="KeyNotFoundException">Thrown when the contract does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the status transition is not allowed.</exception>
    Task UpdateStatusAsync(int contractId, ContractStatus newStatus);

    /// <summary>
    /// Persists arbitrary changes to a contract entity (e.g. attaching a file path).
    /// </summary>
    /// <param name="contract">The modified contract entity.</param>
    Task UpdateContractAsync(Contract contract);

    /// <summary>
    /// Validates whether a status transition is permitted without performing it.
    /// Delegates to the injected <see cref="IValidationStrategy{T}"/>.
    /// </summary>
    /// <param name="current">The contract's current status.</param>
    /// <param name="newStatus">The proposed new status.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the transition is valid.</returns>
    ValidationResult ValidateStatusChange(ContractStatus current, ContractStatus newStatus);
}
