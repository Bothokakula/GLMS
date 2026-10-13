// File: Services/ServiceRequestService.cs
// Purpose: Application service for creating and querying service requests.
//          Orchestrates contract-status enforcement, live currency conversion,
//          and validation via the Strategy pattern before persisting a new request.
// References:
//   - Strategy pattern — Gamma, E., Helm, R., Johnson, R. and Vlissides, J. (1994)
//     Design Patterns: Elements of Reusable Object-Oriented Software.
//   - Asynchronous programming with async and await —
//     Microsoft (2024) Asynchronous programming with async and await.

using GLMS.Core.DTOs;
using GLMS.Core.Interfaces;
using GLMS.Core.Models;

namespace GLMS.Core.Services;

/// <summary>
/// Service responsible for the creation and retrieval of
/// <see cref="ServiceRequest"/> entities. Ensures that requests can only be
/// raised against active contracts and that currency conversion parameters
/// pass validation before persistence.
/// </summary>
public class ServiceRequestService : IServiceRequestService
{
    private readonly IServiceRequestRepository _serviceRequestRepository;
    private readonly IContractRepository _contractRepository;
    private readonly ICurrencyService _currencyService;
    private readonly IValidationStrategy<CurrencyConversionRequest> _currencyValidation;

    /// <summary>
    /// Initialises a new instance of <see cref="ServiceRequestService"/>.
    /// </summary>
    /// <param name="serviceRequestRepository">Repository for persisting service requests.</param>
    /// <param name="contractRepository">Repository for looking up parent contracts.</param>
    /// <param name="currencyService">Service for obtaining live exchange rates and converting amounts.</param>
    /// <param name="currencyValidation">
    /// Strategy implementation for validating currency conversion parameters
    /// (Gamma et al., 1994).
    /// </param>
    public ServiceRequestService(
        IServiceRequestRepository serviceRequestRepository,
        IContractRepository contractRepository,
        ICurrencyService currencyService,
        IValidationStrategy<CurrencyConversionRequest> currencyValidation)
    {
        _serviceRequestRepository = serviceRequestRepository;
        _contractRepository = contractRepository;
        _currencyService = currencyService;
        _currencyValidation = currencyValidation;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The method enforces a strict contract-status gate: only
    /// <see cref="ContractStatus.Active"/> contracts accept new requests.
    /// After fetching the live USD→ZAR exchange rate, the Strategy pattern is
    /// used to validate the conversion parameters before the request is persisted.
    /// </remarks>
    public async Task<ServiceRequest> CreateAsync(CreateServiceRequestRequest request)
    {
        var contract = await _contractRepository.GetByIdAsync(request.ContractId)
            ?? throw new KeyNotFoundException($"Contract with ID {request.ContractId} not found.");

        // Only active contracts may receive new service requests.
        if (contract.Status == ContractStatus.Expired)
            throw new InvalidOperationException("Cannot add service requests to an expired contract.");

        if (contract.Status == ContractStatus.OnHold)
            throw new InvalidOperationException("Cannot add service requests to a contract that is on hold.");

        if (contract.Status == ContractStatus.Draft)
            throw new InvalidOperationException("Cannot add service requests to a draft contract. Activate it first.");

        var exchangeRate = await _currencyService.GetExchangeRateAsync("USD", "ZAR");

        // Strategy pattern: validate conversion inputs before performing the calculation.
        var currencyValidation = _currencyValidation.Validate(new CurrencyConversionRequest
        {
            Amount = request.CostUSD,
            ExchangeRate = exchangeRate
        });

        if (!currencyValidation.IsValid)
        {
            throw new InvalidOperationException(
                $"Currency validation failed: {string.Join("; ", currencyValidation.Errors)}");
        }

        var costZAR = _currencyService.ConvertCurrency(request.CostUSD, exchangeRate);

        var serviceRequest = new ServiceRequest
        {
            ContractId = request.ContractId,
            Description = request.Description,
            CostUSD = request.CostUSD,
            CostZAR = costZAR,
            ExchangeRate = exchangeRate,
            Status = "Pending",
            CreatedDate = DateTime.UtcNow
        };

        return await _serviceRequestRepository.AddAsync(serviceRequest);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceRequest>> GetByContractIdAsync(int contractId)
    {
        return await _serviceRequestRepository.GetByContractIdAsync(contractId);
    }
}
