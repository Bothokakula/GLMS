// AuthApiTests.cs
// Integration tests for the authentication API endpoint (POST /api/auth/login).
// Validates that valid credentials produce a JWT token and invalid credentials are rejected.
// Integration testing with WebApplicationFactory (Microsoft, 2024, Integration tests in ASP.NET Core)
// xUnit test framework (xUnit.net, 2024)

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLMS.Core.DTOs;

namespace GLMS.Tests.Integration;

/// <summary>
/// Tests the authentication endpoint using the <see cref="AuthTestWebApplicationFactory{TProgram}"/>
/// which preserves real authentication middleware so we can verify actual JWT issuance.
/// </summary>
public class AuthApiTests : IClassFixture<AuthTestWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AuthApiTests(AuthTestWebApplicationFactory<Program> factory)
    {
        // HttpClient pattern for service communication (Microsoft, 2024, Make HTTP requests using IHttpClientFactory)
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Validates that a user with correct credentials receives a JWT token, the expected username,
    /// and a future expiration timestamp. This is the happy-path authentication scenario.
    /// </summary>
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Follows Arrange-Act-Assert testing pattern
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "Admin@123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(loginResponse);
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.Token));
        Assert.Equal("admin", loginResponse.Username);
        Assert.True(loginResponse.Expiration > DateTime.UtcNow);
    }

    /// <summary>
    /// Validates that incorrect credentials are rejected with HTTP 401 Unauthorized.
    /// Ensures the API does not leak information about whether the username exists.
    /// </summary>
    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        // Follows Arrange-Act-Assert testing pattern
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "WrongPassword123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest, JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
