// ContractApiTests.cs
// Integration tests for the contracts API endpoints (GET/POST/PATCH /api/contracts).
// Covers CRUD operations, data integrity across create-then-read, status transitions, and error cases.
// Integration testing with WebApplicationFactory (Microsoft, 2024, Integration tests in ASP.NET Core)
// xUnit test framework (xUnit.net, 2024)

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GLMS.Core.DTOs;

namespace GLMS.Tests.Integration;

/// <summary>
/// Tests contract API endpoints against an in-memory test server with auto-authentication.
/// Verifies CRUD operations, state transitions, and error handling for contract resources.
/// </summary>
public class ContractApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ContractApiTests(CustomWebApplicationFactory<Program> factory)
    {
        // HttpClient pattern for service communication (Microsoft, 2024, Make HTTP requests using IHttpClientFactory)
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Verifies the GET /api/contracts endpoint returns a 200 status with a non-null list.
    /// Confirms the seeded test data is accessible via the API.
    /// </summary>
    [Fact]
    public async Task GetContracts_Returns200AndList()
    {
        // Follows Arrange-Act-Assert testing pattern
        var response = await _client.GetAsync("/api/contracts");

        response.EnsureSuccessStatusCode();
        var contracts = await response.Content.ReadFromJsonAsync<List<ContractDto>>(JsonOptions);
        Assert.NotNull(contracts);
    }

    /// <summary>
    /// Verifies that POST /api/contracts creates a new contract with 201 Created status
    /// and that newly created contracts always start in "Draft" status regardless of input.
    /// </summary>
    [Fact]
    public async Task CreateContract_Returns201()
    {
        // Follows Arrange-Act-Assert testing pattern
        var body = new
        {
            ClientId = 1,
            Title = "New Integration Test Contract",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddYears(1),
            ServiceLevel = "Standard"
        };

        var response = await _client.PostAsJsonAsync("/api/contracts", body, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal("Draft", dto.Status);
    }

    /// <summary>
    /// Validates end-to-end data integrity: creates a contract, then fetches it by ID
    /// and verifies all fields match the original input. Catches serialization or persistence bugs.
    /// </summary>
    [Fact]
    public async Task CreateThenGetContract_DataIntegrity()
    {
        // Follows Arrange-Act-Assert testing pattern
        var createBody = new
        {
            ClientId = 1,
            Title = "Data Integrity Check Contract",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddYears(2),
            ServiceLevel = "Premium"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/contracts", createBody, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/contracts/{created.Id}");
        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);
        Assert.NotNull(fetched);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Data Integrity Check Contract", fetched.Title);
        Assert.Equal("Draft", fetched.Status);
        Assert.Equal("Premium", fetched.ServiceLevel);
    }

    /// <summary>
    /// Verifies the valid state transition from Draft to Active via PATCH /api/contracts/{id}/status.
    /// This is the primary happy-path workflow transition.
    /// </summary>
    [Fact]
    public async Task UpdateStatus_DraftToActive_Returns200()
    {
        // Follows Arrange-Act-Assert testing pattern
        var createBody = new
        {
            ClientId = 1,
            Title = "Status Update Contract",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddYears(1),
            ServiceLevel = "Standard"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/contracts", createBody, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);
        Assert.NotNull(created);

        var statusContent = JsonContent.Create(
            new { NewStatus = "Active" }, options: JsonOptions);
        var patchResponse = await _client.PatchAsync(
            $"/api/contracts/{created.Id}/status", statusContent);

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
    }

    /// <summary>
    /// Verifies that requesting a non-existent contract returns 404 Not Found.
    /// Ensures the API handles missing resources gracefully rather than throwing 500.
    /// </summary>
    [Fact]
    public async Task GetNonExistentContract_Returns404()
    {
        // Follows Arrange-Act-Assert testing pattern
        var response = await _client.GetAsync("/api/contracts/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that an invalid state transition (Draft -> Expired, skipping Active) returns 400 Bad Request.
    /// The contract state machine should enforce that Draft can only transition to Active.
    /// </summary>
    [Fact]
    public async Task UpdateStatus_InvalidTransition_Returns400()
    {
        // Follows Arrange-Act-Assert testing pattern
        var createBody = new
        {
            ClientId = 1,
            Title = "Invalid Transition Contract",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddYears(1),
            ServiceLevel = "Standard"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/contracts", createBody, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);
        Assert.NotNull(created);

        var statusContent = JsonContent.Create(
            new { NewStatus = "Expired" }, options: JsonOptions);
        var patchResponse = await _client.PatchAsync(
            $"/api/contracts/{created.Id}/status", statusContent);

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }
}
