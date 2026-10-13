// File: Services/ContractService.cs
// Purpose: Application service orchestrating the full contract lifecycle. Combines
//          three Gang-of-Four design patterns to keep responsibilities separated:
//            • Factory Method  — contract creation is delegated to IContractFactory.
//            • Strategy        — status-transition validation is delegated to
//                                IValidationStrategy<StatusChangeRequest>.
//            • Observer        — after a status change, all registered IContractObserver
//                                instances are notified so side-effects (audit logging,
//                                cascading status updates) are decoupled from core logic.
// References:
//   - Factory Method, Strategy, and Observer patterns — Gamma, E., Helm, R.,
//     Johnson, R. and Vlissides, J. (1994) Design Patterns: Elements of Reusable
//     Object-Oriented Software.
//   - Asynchronous programming with async and await —
//     Microsoft (2024) Asynchronous programming with async and await.

using GLMS.Core.DTOs;
using GLMS.Core.Interfaces;
using GLMS.Core.Models;

namespace GLMS.Core.Services;

/// <summary>
/// Core service responsible for creating, querying, and managing the lifecycle
/// of <see cref="Contract"/> entities. Uses constructor-injected collaborators
/// to keep the class open for extension without modification (Open/Closed Principle).
/// </summary>
public class ContractService : IContractService
{
    private readonly IContractRepository _contractRepository;
    private readonly IContractFactory _contractFactory;
    private readonly IValidationStrategy<StatusChangeRequest> _statusValidation;
    private readonly IEnumerable<IContractObserver> _observers;

    /// <summary>
    /// Initialises a new instance of <see cref="ContractService"/>.
    /// </summary>
    /// <param name="contractRepository">Repository for contract persistence.</param>
    /// <param name="contractFactory">
    /// Factory Method implementation that encapsulates contract construction rules
    /// (Gamma et al., 1994).
    /// </param>
    /// <param name="statusValidation">
    /// Strategy implementation that determines which status transitions are valid
    /// (Gamma et al., 1994).
    /// </param>
    /// <param name="observers">
    /// Collection of Observer implementations notified after a status change
    /// (Gamma et al., 1994). Injected as <see cref="IEnumerable{T}"/> so new
    /// observers can be registered in DI without modifying this class.
    /// </param>
    public ContractService(
        IContractRepository contractRepository,
        IContractFactory contractFactory,
        IValidationStrategy<StatusChangeRequest> statusValidation,
        IEnumerable<IContractObserver> observers)
    {
        _contractRepository = contractRepository;
        _contractFactory = contractFactory;
        _statusValidation = statusValidation;
        _observers = observers;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Delegates construction to the <see cref="IContractFactory"/> (Factory Method
    /// pattern) so default-duration rules and initial status are applied consistently.
    /// </remarks>
    public async Task<Contract> CreateContractAsync(CreateContractRequest request)
    {
        // Factory Method pattern: construction details are encapsulated in the factory.
        var contract = _contractFactory.CreateContract(
            request.ServiceLevel,
            request.ClientId,
            request.Title,
            request.StartDate,
            request.EndDate);

        return await _contractRepository.AddAsync(contract);
    }

    /// <inheritdoc />
    public async Task<Contract?> GetByIdAsync(int id)
    {
        return await _contractRepository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Contract>> GetAllAsync(ContractFilter filter)
    {
        return await _contractRepository.GetAllAsync(filter);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Applies the Strategy pattern for validation and the Observer pattern for
    /// post-transition side-effects. Each observer is awaited sequentially to
    /// guarantee ordering; parallelisation can be added if observers are
    /// confirmed to be independent.
    /// </remarks>
    public async Task UpdateStatusAsync(int contractId, ContractStatus newStatus)
    {
        var contract = await _contractRepository.GetByIdAsync(contractId)
            ?? throw new KeyNotFoundException($"Contract with ID {contractId} not found.");

        var oldStatus = contract.Status;

        // Strategy pattern: validation logic is encapsulated in a separate strategy class.
        var validation = ValidateStatusChange(oldStatus, newStatus);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Invalid status transition: {string.Join("; ", validation.Errors)}");
        }

        contract.Status = newStatus;
        await _contractRepository.UpdateAsync(contract);

        // Observer pattern: notify all registered observers of the status change.
        foreach (var observer in _observers)
        {
            await observer.OnStatusChanged(contractId, oldStatus, newStatus);
        }
    }

    /// <inheritdoc />
    public async Task UpdateContractAsync(Contract contract)
    {
        await _contractRepository.UpdateAsync(contract);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Strategy pattern: delegates to the injected <see cref="IValidationStrategy{T}"/>
    /// so the set of valid transitions can be changed by swapping the strategy
    /// implementation in the DI container.
    /// </remarks>
    public ValidationResult ValidateStatusChange(ContractStatus current, ContractStatus newStatus)
    {
        return _statusValidation.Validate(new StatusChangeRequest
        {
            CurrentStatus = current,
            NewStatus = newStatus
        });
    }
}
