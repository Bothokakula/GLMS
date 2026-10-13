// ServiceRequestApiTests.cs
// Integration tests for the service request API endpoints (POST /api/servicerequests).
// Validates that service requests can only be created against active contracts,
// and that expired contracts reject new requests (business rule enforcement).
// Integration testing with WebApplicationFactory (Microsoft, 2024, Integration tests in ASP.NET Core)
// xUnit test framework (xUnit.net, 2024)

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GLMS.Core.DTOs;

namespace GLMS.Tests.Integration;

/// <summary>
/// Tests service request creation against the API, focusing on contract-status-dependent business rules.
/// Each test sets up its own client and contract to avoid cross-test interference.
/// </summary>
public class ServiceRequestApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ServiceRequestApiTests(CustomWebApplicationFactory<Program> factory)
    {
        // HttpClient pattern for service communication (Microsoft, 2024, Make HTTP requests using IHttpClientFactory)
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Helper that creates a fresh client, a contract, and activates it (Draft -> Active).
    /// Provides each test with an isolated active contract to avoid shared-state issues between tests.
    /// </summary>
    /// <returns>The ID of the newly created and activated contract.</returns>
    private async Task<int> CreateActiveContractAsync()
    {
        var clientBody = new
        {
            Name = $"SR Client {Guid.NewGuid():N}",
            Email = $"sr-{Guid.NewGuid():N}@test.com",
            Phone = "555-1234",
            Region = "Western Cape"
        };
        var clientResponse = await _client.PostAsJsonAsync("/api/clients", clientBody, JsonOptions);
        clientResponse.EnsureSuccessStatusCode();
        var clientDto = await clientResponse.Content.ReadFromJsonAsync<ClientDto>(JsonOptions);

        var contractBody = new
        {
            ClientId = clientDto!.Id,
            Title = $"SR Test Contract {Guid.NewGuid():N}",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddYears(1),
            ServiceLevel = "Standard"
        };
        var contractResponse = await _client.PostAsJsonAsync("/api/contracts", contractBody, JsonOptions);
        contractResponse.EnsureSuccessStatusCode();
        var contractDto = await contractResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);

        var activateContent = JsonContent.Create(
            new { NewStatus = "Active" }, options: JsonOptions);
        var activateResponse = await _client.PatchAsync(
            $"/api/contracts/{contractDto!.Id}/status", activateContent);
        activateResponse.EnsureSuccessStatusCode();

        return contractDto.Id;
    }

    /// <summary>
    /// Validates that a service request can be created against an active contract.
    /// Verifies the response includes the correct contract ID, USD cost, a positive ZAR conversion,
    /// and a "Pending" initial status. This is the happy-path business rule.
    /// </summary>
    [Fact]
    public async Task CreateServiceRequest_ForActiveContract_Returns201()
    {
        // Follows Arrange-Act-Assert testing pattern
        var contractId = await CreateActiveContractAsync();

        var srBody = new
        {
            ContractId = contractId,
            Description = "Test service request for active contract",
            CostUSD = 150.00m
        };

        var response = await _client.PostAsJsonAsync("/api/servicerequests", srBody, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ServiceRequestDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(contractId, dto.ContractId);
        Assert.Equal(150.00m, dto.CostUSD);
        Assert.True(dto.CostZAR > 0);
        Assert.Equal("Pending", dto.Status);
    }

    /// <summary>
    /// Validates the business rule that service requests cannot be created against expired contracts.
    /// First activates a contract, then expires it, then attempts to create a service request.
    /// Expects 409 Conflict because the business logic forbids operations on expired contracts.
    /// </summary>
    [Fact]
    public async Task CreateServiceRequest_ForExpiredContract_Returns409()
    {
        // Follows Arrange-Act-Assert testing pattern
        var contractId = await CreateActiveContractAsync();

        // Transition the contract to Expired (Active -> Expired is a valid transition).
        var expireContent = JsonContent.Create(
            new { NewStatus = "Expired" }, options: JsonOptions);
        var expireResponse = await _client.PatchAsync(
            $"/api/contracts/{contractId}/status", expireContent);
        expireResponse.EnsureSuccessStatusCode();

        var srBody = new
        {
            ContractId = contractId,
            Description = "Should fail on expired contract",
            CostUSD = 100.00m
        };

        var response = await _client.PostAsJsonAsync("/api/servicerequests", srBody, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
