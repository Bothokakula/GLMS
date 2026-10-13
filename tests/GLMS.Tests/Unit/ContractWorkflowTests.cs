// ContractWorkflowTests.cs
// Unit tests for contract status validation (state machine transitions) and service request workflow logic.
// Contains two test classes:
//   - ContractStatusValidationTests: Validates allowed/disallowed state transitions in the contract lifecycle.
//   - ContractWorkflowServiceTests: Tests service request creation rules and the observer notification pattern.
// xUnit test framework (xUnit.net, 2024)
// Moq mocking framework for unit test isolation (Moq, 2024)

using GLMS.Core.DTOs;
using GLMS.Core.Interfaces;
using GLMS.Core.Models;
using GLMS.Core.Services;
using GLMS.Core.Services.Validation;
using Moq;

namespace GLMS.Tests.Unit;

/// <summary>
/// Tests the contract status state machine to ensure only valid transitions are allowed.
/// The contract lifecycle is: Draft -> Active -> (Expired | OnHold), OnHold -> Active.
/// Invalid transitions (e.g., Expired -> Active, Draft -> Expired) must be rejected.
/// </summary>
public class ContractStatusValidationTests
{
    private readonly ContractStatusValidationStrategy _strategy = new();

    /// <summary>
    /// Draft -> Active is the primary activation path. Must be valid.
    /// </summary>
    [Fact]
    public void Validate_DraftToActive_IsValid()
    {
        // Follows Arrange-Act-Assert testing pattern
        var request = new StatusChangeRequest
        {
            CurrentStatus = ContractStatus.Draft,
            NewStatus = ContractStatus.Active
        };

        var result = _strategy.Validate(request);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Active -> Expired represents natural contract termination. Must be valid.
    /// </summary>
    [Fact]
    public void Validate_ActiveToExpired_IsValid()
    {
        var request = new StatusChangeRequest
        {
            CurrentStatus = ContractStatus.Active,
            NewStatus = ContractStatus.Expired
        };

        var result = _strategy.Validate(request);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Active -> OnHold represents a temporary suspension (e.g., payment dispute). Must be valid.
    /// </summary>
    [Fact]
    public void Validate_ActiveToOnHold_IsValid()
    {
        var request = new StatusChangeRequest
        {
            CurrentStatus = ContractStatus.Active,
            NewStatus = ContractStatus.OnHold
        };

        var result = _strategy.Validate(request);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// OnHold -> Active represents reactivation after resolving the hold. Must be valid.
    /// </summary>
    [Fact]
    public void Validate_OnHoldToActive_IsValid()
    {
        var request = new StatusChangeRequest
        {
            CurrentStatus = ContractStatus.OnHold,
            NewStatus = ContractStatus.Active
        };

        var result = _strategy.Validate(request);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Expired -> Active is disallowed because expired contracts cannot be reactivated;
    /// a new contract must be created instead.
    /// </summary>
    [Fact]
    public void Validate_ExpiredToActive_IsInvalid()
    {
        var request = new StatusChangeRequest
        {
            CurrentStatus = ContractStatus.Expired,
            NewStatus = ContractStatus.Active
        };

        var result = _strategy.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Cannot transition"));
    }

    /// <summary>
    /// Draft -> Expired is disallowed because a contract must be activated before it can expire.
    /// </summary>
    [Fact]
    public void Validate_DraftToExpired_IsInvalid()
    {
        var request = new StatusChangeRequest
        {
            CurrentStatus = ContractStatus.Draft,
            NewStatus = ContractStatus.Expired
        };

        var result = _strategy.Validate(request);

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Draft -> OnHold is disallowed because a contract must be activated before it can be suspended.
    /// </summary>
    [Fact]
    public void Validate_DraftToOnHold_IsInvalid()
    {
        var request = new StatusChangeRequest
        {
            CurrentStatus = ContractStatus.Draft,
            NewStatus = ContractStatus.OnHold
        };

        var result = _strategy.Validate(request);

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// OnHold -> Expired is disallowed; a held contract must be reactivated before it can expire.
    /// </summary>
    [Fact]
    public void Validate_OnHoldToExpired_IsInvalid()
    {
        var request = new StatusChangeRequest
        {
            CurrentStatus = ContractStatus.OnHold,
            NewStatus = ContractStatus.Expired
        };

        var result = _strategy.Validate(request);

        Assert.False(result.IsValid);
    }
}

/// <summary>
/// Tests the <see cref="ServiceRequestService"/> and <see cref="ContractService"/> workflow logic,
/// including contract-status-based guards on service request creation and the observer notification pattern.
/// All repository and service dependencies are mocked to isolate business logic.
/// </summary>
public class ContractWorkflowServiceTests
{
    // Moq mocking framework for unit test isolation (Moq, 2024)
    private readonly Mock<IContractRepository> _mockContractRepo;
    private readonly Mock<IServiceRequestRepository> _mockServiceRequestRepo;
    private readonly Mock<ICurrencyService> _mockCurrencyService;
    private readonly Mock<IValidationStrategy<CurrencyConversionRequest>> _mockCurrencyValidation;
    private readonly Mock<IContractFactory> _mockContractFactory;
    private readonly Mock<IValidationStrategy<StatusChangeRequest>> _mockStatusValidation;
    private readonly ServiceRequestService _serviceRequestService;

    public ContractWorkflowServiceTests()
    {
        _mockContractRepo = new Mock<IContractRepository>();
        _mockServiceRequestRepo = new Mock<IServiceRequestRepository>();
        _mockCurrencyService = new Mock<ICurrencyService>();
        _mockCurrencyValidation = new Mock<IValidationStrategy<CurrencyConversionRequest>>();
        _mockContractFactory = new Mock<IContractFactory>();
        _mockStatusValidation = new Mock<IValidationStrategy<StatusChangeRequest>>();

        _serviceRequestService = new ServiceRequestService(
            _mockServiceRequestRepo.Object,
            _mockContractRepo.Object,
            _mockCurrencyService.Object,
            _mockCurrencyValidation.Object);
    }

    /// <summary>
    /// Verifies that creating a service request on an expired contract throws InvalidOperationException.
    /// Business rule: expired contracts are finalized and cannot accept new work.
    /// </summary>
    [Fact]
    public async Task CreateServiceRequest_OnExpiredContract_ThrowsInvalidOperation()
    {
        // Follows Arrange-Act-Assert testing pattern
        var contract = new Contract { Id = 1, Status = ContractStatus.Expired };
        _mockContractRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var request = new CreateServiceRequestRequest
        {
            ContractId = 1,
            Description = "Test request",
            CostUSD = 100
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _serviceRequestService.CreateAsync(request));
        Assert.Contains("expired", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that creating a service request on a held contract throws InvalidOperationException.
    /// Business rule: on-hold contracts are suspended and cannot accept new work until reactivated.
    /// </summary>
    [Fact]
    public async Task CreateServiceRequest_OnOnHoldContract_ThrowsInvalidOperation()
    {
        var contract = new Contract { Id = 2, Status = ContractStatus.OnHold };
        _mockContractRepo
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var request = new CreateServiceRequestRequest
        {
            ContractId = 2,
            Description = "Test request",
            CostUSD = 50
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _serviceRequestService.CreateAsync(request));
        Assert.Contains("on hold", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies the happy path: a service request on an active contract is successfully created
    /// with correct currency conversion (USD -> ZAR) and persisted to the repository.
    /// Confirms the returned DTO contains the correct contract ID, ZAR amount, exchange rate, and status.
    /// </summary>
    [Fact]
    public async Task CreateServiceRequest_OnActiveContract_Succeeds()
    {
        // Follows Arrange-Act-Assert testing pattern
        var contract = new Contract { Id = 3, Status = ContractStatus.Active };
        _mockContractRepo
            .Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        _mockCurrencyService
            .Setup(s => s.GetExchangeRateAsync("USD", "ZAR"))
            .ReturnsAsync(18.5m);
        _mockCurrencyService
            .Setup(s => s.ConvertCurrency(100m, 18.5m))
            .Returns(1850m);

        _mockCurrencyValidation
            .Setup(v => v.Validate(It.IsAny<CurrencyConversionRequest>()))
            .Returns(ValidationResult.Success());

        _mockServiceRequestRepo
            .Setup(r => r.AddAsync(It.IsAny<ServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceRequest sr, CancellationToken _) =>
            {
                sr.Id = 1;
                return sr;
            });

        var request = new CreateServiceRequestRequest
        {
            ContractId = 3,
            Description = "Active contract service request",
            CostUSD = 100
        };

        var result = await _serviceRequestService.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal(3, result.ContractId);
        Assert.Equal(1850m, result.CostZAR);
        Assert.Equal(18.5m, result.ExchangeRate);
        Assert.Equal("Pending", result.Status);
        _mockServiceRequestRepo.Verify(
            r => r.AddAsync(It.IsAny<ServiceRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies the Observer pattern: when a contract's status is updated, ALL registered
    /// observers are notified exactly once with the correct contract ID, old status, and new status.
    /// This ensures audit logging, notifications, and other cross-cutting concerns are triggered.
    /// </summary>
    [Fact]
    public async Task UpdateStatus_NotifiesAllObservers()
    {
        // Moq mocking framework for unit test isolation (Moq, 2024)
        var observer1 = new Mock<IContractObserver>();
        var observer2 = new Mock<IContractObserver>();

        var contract = new Contract { Id = 1, Status = ContractStatus.Draft };
        _mockContractRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        _mockStatusValidation
            .Setup(v => v.Validate(It.IsAny<StatusChangeRequest>()))
            .Returns(ValidationResult.Success());

        _mockContractRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Contract>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        observer1
            .Setup(o => o.OnStatusChanged(It.IsAny<int>(), It.IsAny<ContractStatus>(), It.IsAny<ContractStatus>()))
            .Returns(Task.CompletedTask);
        observer2
            .Setup(o => o.OnStatusChanged(It.IsAny<int>(), It.IsAny<ContractStatus>(), It.IsAny<ContractStatus>()))
            .Returns(Task.CompletedTask);

        var contractService = new ContractService(
            _mockContractRepo.Object,
            _mockContractFactory.Object,
            _mockStatusValidation.Object,
            new[] { observer1.Object, observer2.Object });

        await contractService.UpdateStatusAsync(1, ContractStatus.Active);

        observer1.Verify(
            o => o.OnStatusChanged(1, ContractStatus.Draft, ContractStatus.Active),
            Times.Once);
        observer2.Verify(
            o => o.OnStatusChanged(1, ContractStatus.Draft, ContractStatus.Active),
            Times.Once);
    }
}
