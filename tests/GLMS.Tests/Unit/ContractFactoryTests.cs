// ContractFactoryTests.cs
// Unit tests for the ContractFactory, which creates Contract entities with correct defaults
// based on service level. Validates default status, date assignments, and automatic end-date
// correction when an invalid (past) end date is provided.
// xUnit test framework (xUnit.net, 2024)

using GLMS.Core.Models;
using GLMS.Core.Services;

namespace GLMS.Tests.Unit;

/// <summary>
/// Tests the <see cref="ContractFactory"/> to ensure contracts are created with the correct
/// default values, and that invalid end dates trigger automatic correction based on service level.
/// </summary>
public class ContractFactoryTests
{
    private readonly ContractFactory _factory = new();

    /// <summary>
    /// Verifies that a Standard-level contract is initialized with Draft status,
    /// the correct service level, and exact start/end dates from input.
    /// </summary>
    [Fact]
    public void CreateContract_Standard_SetsCorrectDefaults()
    {
        // Follows Arrange-Act-Assert testing pattern
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2027, 1, 1);

        var contract = _factory.CreateContract(
            ServiceLevel.Standard, 1, "Standard Contract", start, end);

        Assert.Equal(ContractStatus.Draft, contract.Status);
        Assert.Equal(ServiceLevel.Standard, contract.ServiceLevel);
        Assert.Equal(start, contract.StartDate);
        Assert.Equal(end, contract.EndDate);
    }

    /// <summary>
    /// Verifies that a Premium-level contract is initialized with Draft status
    /// and the correct Premium service level.
    /// </summary>
    [Fact]
    public void CreateContract_Premium_SetsCorrectDefaults()
    {
        // Follows Arrange-Act-Assert testing pattern
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2028, 1, 1);

        var contract = _factory.CreateContract(
            ServiceLevel.Premium, 1, "Premium Contract", start, end);

        Assert.Equal(ContractStatus.Draft, contract.Status);
        Assert.Equal(ServiceLevel.Premium, contract.ServiceLevel);
        Assert.Equal(start, contract.StartDate);
        Assert.Equal(end, contract.EndDate);
    }

    /// <summary>
    /// Verifies that the contract title is assigned exactly as provided.
    /// </summary>
    [Fact]
    public void CreateContract_SetsTitle()
    {
        var contract = _factory.CreateContract(
            ServiceLevel.Standard, 1, "My Important Contract",
            DateTime.UtcNow, DateTime.UtcNow.AddYears(1));

        Assert.Equal("My Important Contract", contract.Title);
    }

    /// <summary>
    /// Verifies that the client ID is assigned correctly, establishing the foreign key relationship.
    /// </summary>
    [Fact]
    public void CreateContract_SetsClientId()
    {
        var contract = _factory.CreateContract(
            ServiceLevel.Standard, 42, "Test",
            DateTime.UtcNow, DateTime.UtcNow.AddYears(1));

        Assert.Equal(42, contract.ClientId);
    }

    /// <summary>
    /// Verifies that the start date is persisted exactly as provided with no rounding or adjustment.
    /// </summary>
    [Fact]
    public void CreateContract_SetsStartDate()
    {
        var start = new DateTime(2026, 6, 15);

        var contract = _factory.CreateContract(
            ServiceLevel.Standard, 1, "Test",
            start, start.AddYears(1));

        Assert.Equal(start, contract.StartDate);
    }

    /// <summary>
    /// Verifies that the end date is persisted exactly as provided when it is after the start date.
    /// </summary>
    [Fact]
    public void CreateContract_SetsEndDate()
    {
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2027, 6, 30);

        var contract = _factory.CreateContract(
            ServiceLevel.Standard, 1, "Test", start, end);

        Assert.Equal(end, contract.EndDate);
    }

    /// <summary>
    /// Validates the safety net: when a Standard contract is given an end date before the start date,
    /// the factory auto-corrects to start + 1 year (the Standard default duration).
    /// </summary>
    [Fact]
    public void CreateContract_Standard_InvalidEndDate_DefaultsToOneYear()
    {
        var start = new DateTime(2026, 6, 1);
        var endBeforeStart = new DateTime(2025, 1, 1);

        var contract = _factory.CreateContract(
            ServiceLevel.Standard, 1, "Test", start, endBeforeStart);

        Assert.Equal(start.AddYears(1), contract.EndDate);
    }

    /// <summary>
    /// Validates the safety net: when a Premium contract is given an end date before the start date,
    /// the factory auto-corrects to start + 2 years (the Premium default duration).
    /// </summary>
    [Fact]
    public void CreateContract_Premium_InvalidEndDate_DefaultsToTwoYears()
    {
        var start = new DateTime(2026, 6, 1);
        var endBeforeStart = new DateTime(2025, 1, 1);

        var contract = _factory.CreateContract(
            ServiceLevel.Premium, 1, "Test", start, endBeforeStart);

        Assert.Equal(start.AddYears(2), contract.EndDate);
    }
}
