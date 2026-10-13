// ==========================================================================
// File: Program.cs
// Purpose: Application entry point and service composition root for the
//          GLMS (Global Logistics Management System) Web API. Configures
//          all middleware, authentication, dependency injection, Swagger
//          documentation, and database initialization.
// ==========================================================================

// Dependency Injection principle (Martin, R.C., 2003, Agile Software Development)
// RESTful API design (Fielding, R., 2000, Architectural Styles and the Design of Network-based Software Architectures)

using System.Text;
using System.Text.Json.Serialization;
using GLMS.Core.DTOs;
using GLMS.Core.Interfaces;
using GLMS.Core.Services;
using GLMS.Core.Services.Observers;
using GLMS.Core.Services.Validation;
using GLMS.Infrastructure.Data;
using GLMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers and configure JSON serialization to convert enums
// to their string names instead of integer values, improving API readability.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// OpenAPI/Swagger documentation (OpenAPI Initiative, 2024)
// Enable endpoint discovery for Swagger and configure the Swagger document
// with JWT Bearer authentication support so developers can test secured
// endpoints directly from the Swagger UI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "GLMS API", Version = "v1" });

    // Define the Bearer security scheme so Swagger UI presents a token input dialog,
    // allowing authenticated requests without an external HTTP client.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    // Apply the Bearer scheme globally to all endpoints, ensuring Swagger UI
    // sends the Authorization header with every request.
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JWT Bearer authentication (Microsoft, 2024, ASP.NET Core Security documentation)
// Read JWT configuration from appsettings.json. These values are used both here
// (for token validation) and in AuthController (for token generation).
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// Configure the authentication pipeline to validate incoming JWT tokens against
// the same issuer, audience, and signing key used during token creation,
// ensuring only tokens issued by this API are accepted.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Entity Framework Core data access (Microsoft, 2024, EF Core documentation)
// Connection string priority: GLMS_CONNECTION_STRING env var > appsettings.json
// Set a machine-level environment variable to point the API at any SQL Server instance,
// and the database will be auto-created on first run via EnsureCreated() at startup.
var connectionString = Environment.GetEnvironmentVariable("GLMS_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<GlmsDbContext>(options =>
    options.UseSqlServer(connectionString));

// Dependency Injection principle (Martin, R.C., 2003, Agile Software Development)
// Register all application services with scoped lifetime so each HTTP request
// receives its own instance, ensuring thread safety and proper DbContext tracking.

// Repository pattern implementation (Fowler, M., 2002, Patterns of Enterprise Application Architecture)
// Repositories abstract the data access layer, allowing the service layer to remain
// persistence-ignorant and simplifying unit testing through interface-based mocking.
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();

// Domain service registrations — encapsulate business logic separately from controllers.
builder.Services.AddScoped<IContractFactory, ContractFactory>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IFileService>(sp =>
    new FileService(Path.Combine(builder.Environment.ContentRootPath, "uploads")));

// Strategy pattern registrations — each validation strategy can be swapped independently,
// keeping validation rules decoupled from the services that invoke them.
builder.Services.AddScoped<IValidationStrategy<StatusChangeRequest>, ContractStatusValidationStrategy>();
builder.Services.AddScoped<IValidationStrategy<FileUploadRequest>, FileValidationStrategy>();
builder.Services.AddScoped<IValidationStrategy<CurrencyConversionRequest>, CurrencyValidationStrategy>();

// Observer pattern registrations — observers react to contract state changes without
// the contract service needing to know about downstream side effects.
builder.Services.AddScoped<IContractObserver, ServiceRequestStatusObserver>();
builder.Services.AddScoped<IContractObserver, AuditLogObserver>();

// Register HttpClient for CurrencyService to enable external API calls to the
// exchange-rate provider, benefiting from HttpClientFactory connection pooling.
builder.Services.AddHttpClient<ICurrencyService, CurrencyService>();

// Configure CORS to allow any origin during development. In production, this should
// be restricted to known front-end domains to prevent unauthorized cross-origin requests.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// OpenAPI/Swagger documentation (OpenAPI Initiative, 2024)
// Expose Swagger JSON and the interactive UI on all environments for demonstration purposes.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

// JWT Bearer authentication (Microsoft, 2024, ASP.NET Core Security documentation)
// The order matters: authentication must run before authorization so the identity
// is established before access policies are evaluated.
app.UseAuthentication();
app.UseAuthorization();

// RESTful API design (Fielding, R., 2000, Architectural Styles and the Design of Network-based Software Architectures)
// Map attribute-routed controllers to the request pipeline.
app.MapControllers();

// Entity Framework Core data access (Microsoft, 2024, EF Core documentation)
// Ensure the database schema exists on startup. A retry loop handles the case where
// SQL Server is still starting (e.g., in a Docker Compose environment) by waiting
// up to ~50 seconds before giving up.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GlmsDbContext>();
    var retries = 10;
    while (retries > 0)
    {
        try
        {
            db.Database.EnsureCreated();
            break;
        }
        catch (Exception ex) when (retries > 1)
        {
            retries--;
            app.Logger.LogWarning("Database not ready, retrying in 5s... ({Retries} left): {Message}", retries, ex.Message);
            Thread.Sleep(5000);
        }
    }
}

app.Run();
