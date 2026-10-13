// GlmsApiClient.cs
// Typed HTTP client that acts as a gateway between the MVC frontend and the GLMS REST API.
// Every public method maps to a specific API endpoint, serializes requests, and deserializes responses.
// HttpClient pattern for service communication (Microsoft, 2024, Make HTTP requests using IHttpClientFactory)

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GLMS.Core.DTOs;
using GLMS.Core.Models;

namespace GLMS.Web.Services;

/// <summary>
/// Typed HttpClient wrapper that communicates with the GLMS REST API.
/// Handles JWT token injection, request serialization, and response deserialization
/// for all domain operations (auth, contracts, clients, service requests, currency).
/// </summary>
public class GlmsApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes the API client with an injected HttpClient and HTTP context accessor.
    /// </summary>
    /// <param name="http">Typed HttpClient whose base address is configured in Program.cs.</param>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context for session-based token retrieval.</param>
    public GlmsApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Reads the JWT from session and attaches it as a Bearer token on outgoing requests.
    /// Called before every authenticated API call to ensure the backend can authorize the user.
    /// </summary>
    private void SetAuthHeader()
    {
        // Session-based token storage for stateless API authentication
        var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <summary>
    /// Authenticates a user against the API.
    /// Calls POST /api/auth/login with username/password credentials.
    /// Returns null on authentication failure so the caller can display an appropriate error.
    /// </summary>
    /// <param name="request">Login credentials (username and password).</param>
    /// <returns>A <see cref="LoginResponse"/> containing the JWT token on success, or null on failure.</returns>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("api/auth/login", content);
        if (!response.IsSuccessStatusCode)
            return null;

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<LoginResponse>(body, _jsonOptions);
    }

    /// <summary>
    /// Retrieves a filtered list of contracts from the API.
    /// Calls GET /api/contracts with optional query-string filters for status, date range, and client name.
    /// </summary>
    /// <param name="status">Optional contract status filter.</param>
    /// <param name="startDate">Optional lower bound for contract start date.</param>
    /// <param name="endDate">Optional upper bound for contract end date.</param>
    /// <param name="clientName">Optional client name filter (partial match).</param>
    /// <returns>A list of contracts matching the specified filters.</returns>
    public async Task<List<ContractDto>> GetContractsAsync(
        ContractStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? clientName = null)
    {
        SetAuthHeader();

        var query = new List<string>();
        if (status.HasValue) query.Add($"status={status.Value}");
        if (startDate.HasValue) query.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue) query.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(clientName)) query.Add($"clientName={Uri.EscapeDataString(clientName)}");

        var url = "api/contracts";
        if (query.Count > 0) url += "?" + string.Join("&", query);

        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ContractDto>>(body, _jsonOptions) ?? new();
    }

    /// <summary>
    /// Retrieves a single contract by its identifier.
    /// Calls GET /api/contracts/{id}.
    /// Returns null when the contract does not exist (404) rather than throwing.
    /// </summary>
    /// <param name="id">The contract identifier.</param>
    /// <returns>The contract DTO, or null if not found.</returns>
    public async Task<ContractDto?> GetContractAsync(int id)
    {
        SetAuthHeader();

        var response = await _http.GetAsync($"api/contracts/{id}");
        if (!response.IsSuccessStatusCode)
            return null;

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ContractDto>(body, _jsonOptions);
    }

    /// <summary>
    /// Creates a new contract.
    /// Calls POST /api/contracts with the contract details in the request body.
    /// </summary>
    /// <param name="request">Contract creation parameters (client, title, dates, service level).</param>
    /// <returns>The newly created contract DTO.</returns>
    public async Task<ContractDto> CreateContractAsync(CreateContractRequest request)
    {
        SetAuthHeader();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("api/contracts", content);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ContractDto>(body, _jsonOptions)!;
    }

    /// <summary>
    /// Transitions a contract to a new status.
    /// Calls PATCH /api/contracts/{id}/status with the desired new status.
    /// The API enforces valid state transitions (e.g., Draft -> Active is allowed; Draft -> Expired is not).
    /// </summary>
    /// <param name="id">The contract identifier.</param>
    /// <param name="newStatus">The target status to transition to.</param>
    public async Task UpdateContractStatusAsync(int id, ContractStatus newStatus)
    {
        SetAuthHeader();

        var json = JsonSerializer.Serialize(new { NewStatus = newStatus }, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Patch, $"api/contracts/{id}/status")
        {
            Content = content
        };
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Uploads a signed agreement file for a contract.
    /// Calls POST /api/contracts/{id}/upload with multipart form data containing the file.
    /// </summary>
    /// <param name="id">The contract identifier to attach the agreement to.</param>
    /// <param name="fileStream">The file content stream.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="contentType">The MIME content type of the file.</param>
    public async Task UploadAgreementAsync(int id, Stream fileStream, string fileName, string contentType)
    {
        SetAuthHeader();

        using var formContent = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        formContent.Add(streamContent, "file", fileName);

        var response = await _http.PostAsync($"api/contracts/{id}/upload", formContent);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Downloads the signed agreement PDF for a contract.
    /// Calls GET /api/contracts/{id}/download.
    /// Returns null when no agreement has been uploaded (404).
    /// </summary>
    /// <param name="id">The contract identifier.</param>
    /// <returns>A tuple of the file bytes and file name, or null if no agreement exists.</returns>
    public async Task<(byte[] data, string fileName)?> DownloadAgreementAsync(int id)
    {
        SetAuthHeader();

        var response = await _http.GetAsync($"api/contracts/{id}/download");
        if (!response.IsSuccessStatusCode)
            return null;

        var data = await response.Content.ReadAsByteArrayAsync();
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                    ?? response.Content.Headers.ContentDisposition?.FileName
                    ?? "agreement.pdf";
        fileName = fileName.Trim('"');

        return (data, fileName);
    }

    /// <summary>
    /// Retrieves all clients.
    /// Calls GET /api/clients.
    /// </summary>
    /// <returns>A list of all client DTOs.</returns>
    public async Task<List<ClientDto>> GetClientsAsync()
    {
        SetAuthHeader();

        var response = await _http.GetAsync("api/clients");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ClientDto>>(body, _jsonOptions) ?? new();
    }

    /// <summary>
    /// Retrieves a single client by identifier.
    /// Calls GET /api/clients/{id}.
    /// Returns null when the client does not exist (404).
    /// </summary>
    /// <param name="id">The client identifier.</param>
    /// <returns>The client DTO, or null if not found.</returns>
    public async Task<ClientDto?> GetClientAsync(int id)
    {
        SetAuthHeader();

        var response = await _http.GetAsync($"api/clients/{id}");
        if (!response.IsSuccessStatusCode)
            return null;

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ClientDto>(body, _jsonOptions);
    }

    /// <summary>
    /// Creates a new client.
    /// Calls POST /api/clients with client details.
    /// </summary>
    /// <param name="request">Client creation parameters (name, email, phone, region).</param>
    /// <returns>The newly created client DTO.</returns>
    public async Task<ClientDto> CreateClientAsync(CreateClientRequest request)
    {
        SetAuthHeader();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("api/clients", content);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ClientDto>(body, _jsonOptions)!;
    }

    /// <summary>
    /// Retrieves all service requests associated with a given contract.
    /// Calls GET /api/servicerequests?contractId={contractId}.
    /// </summary>
    /// <param name="contractId">The contract identifier to filter service requests by.</param>
    /// <returns>A list of service request DTOs for the specified contract.</returns>
    public async Task<List<ServiceRequestDto>> GetServiceRequestsAsync(int contractId)
    {
        SetAuthHeader();

        var response = await _http.GetAsync($"api/servicerequests?contractId={contractId}");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ServiceRequestDto>>(body, _jsonOptions) ?? new();
    }

    /// <summary>
    /// Creates a new service request under a contract.
    /// Calls POST /api/servicerequests with the request details.
    /// The API performs currency conversion (USD to ZAR) server-side.
    /// </summary>
    /// <param name="request">Service request creation parameters (contract ID, description, cost in USD).</param>
    /// <returns>The newly created service request DTO with populated ZAR cost and exchange rate.</returns>
    public async Task<ServiceRequestDto> CreateServiceRequestAsync(CreateServiceRequestRequest request)
    {
        SetAuthHeader();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("api/servicerequests", content);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ServiceRequestDto>(body, _jsonOptions)!;
    }

    /// <summary>
    /// Fetches the current exchange rate between two currencies.
    /// Calls GET /api/currency/rate?from={from}&amp;to={to}.
    /// Used on the service request creation form to show a live USD-to-ZAR preview.
    /// </summary>
    /// <param name="from">Source currency code (e.g., "USD").</param>
    /// <param name="to">Target currency code (e.g., "ZAR").</param>
    /// <returns>The current exchange rate as a decimal.</returns>
    public async Task<decimal> GetExchangeRateAsync(string from, string to)
    {
        SetAuthHeader();

        var response = await _http.GetAsync($"api/currency/rate?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);
        return result.GetProperty("rate").GetDecimal();
    }
}
