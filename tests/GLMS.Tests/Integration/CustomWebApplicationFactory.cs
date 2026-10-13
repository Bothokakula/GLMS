// CustomWebApplicationFactory.cs
// Provides test server configurations for integration testing the GLMS API.
// Contains two factory variants:
//   - CustomWebApplicationFactory: Uses a fake auth handler (auto-authenticates) for testing protected endpoints.
//   - AuthTestWebApplicationFactory: Uses real authentication with seeded admin credentials for auth flow tests.
// Integration testing with WebApplicationFactory (Microsoft, 2024, Integration tests in ASP.NET Core)

using System.Security.Claims;
using System.Text.Encodings.Web;
using GLMS.Core.Interfaces;
using GLMS.Core.Models;
using GLMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLMS.Tests.Integration;

/// <summary>
/// Test server factory that replaces the real database with an in-memory provider,
/// substitutes authentication with a passthrough handler, and seeds baseline test data.
/// Used by most integration tests that need a pre-authenticated context.
/// </summary>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    // Shared in-memory DB root ensures all scopes within a single test see the same data.
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    /// <summary>
    /// Replaces production services with test doubles: in-memory DB, fake auth, and a stub currency service.
    /// </summary>
    /// <param name="builder">The web host builder to configure.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the production DbContext registration so we can substitute the in-memory provider.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<GlmsDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<GlmsDbContext>(options =>
                options.UseInMemoryDatabase("TestDb", _dbRoot));

            // Bypass JWT authentication with a test handler that always succeeds.
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);

            // Stub the currency service to avoid external API calls during tests.
            services.AddScoped<ICurrencyService>(_ => new TestCurrencyService());

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GlmsDbContext>();
            db.Database.EnsureCreated();
            SeedTestData(db);
        });
    }

    /// <summary>
    /// Seeds the in-memory database with a minimal client and contract so integration
    /// tests have baseline data without needing setup in every test method.
    /// </summary>
    /// <param name="db">The database context to seed.</param>
    private static void SeedTestData(GlmsDbContext db)
    {
        if (db.Clients.Any()) return;

        var client = new Client
        {
            Name = "Test Client",
            Email = "test@example.com",
            Phone = "0123456789",
            Region = "Gauteng"
        };
        db.Clients.Add(client);
        db.SaveChanges();

        var contract = new Contract
        {
            ClientId = client.Id,
            Title = "Test Contract",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddYears(1),
            Status = ContractStatus.Draft,
            ServiceLevel = ServiceLevel.Standard
        };
        db.Contracts.Add(contract);
        db.SaveChanges();
    }
}

/// <summary>
/// Fake authentication handler that always returns a successful result with admin claims.
/// Allows integration tests to bypass JWT validation while still having a populated ClaimsPrincipal.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// Returns a successful authentication result with predefined admin claims,
    /// eliminating the need for real JWT tokens in integration tests.
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("sub", "1")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Stub currency service that returns deterministic exchange rates and conversions.
/// Prevents integration tests from depending on the live exchange rate API.
/// </summary>
internal class TestCurrencyService : ICurrencyService
{
    public Task<decimal> GetExchangeRateAsync(string from, string to) =>
        Task.FromResult(18.5m);

    public decimal ConvertCurrency(decimal amount, decimal rate) =>
        Math.Round(amount * rate, 2);
}

/// <summary>
/// Test server factory specifically for authentication flow tests.
/// Unlike <see cref="CustomWebApplicationFactory{TProgram}"/>, this does NOT replace the auth handler,
/// so tests can exercise the real JWT login endpoint with seeded admin credentials.
/// </summary>
public class AuthTestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    /// <summary>
    /// Replaces the production DB with an in-memory provider and seeds an admin user
    /// with a known password for authentication tests. Keeps real auth middleware intact.
    /// </summary>
    /// <param name="builder">The web host builder to configure.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<GlmsDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<GlmsDbContext>(options =>
                options.UseInMemoryDatabase("AuthTestDb", _dbRoot));

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GlmsDbContext>();
            db.Database.EnsureCreated();

            // Ensure a known admin user exists with a predictable password for test assertions.
            var existingAdmin = db.Users.FirstOrDefault(u => u.Username == "admin");
            if (existingAdmin != null)
            {
                existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                db.SaveChanges();
            }
            else
            {
                db.Users.Add(new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "Admin"
                });
                db.SaveChanges();
            }
        });
    }
}
