# Global Logistics Management System (GLMS)

[![CI](https://github.com/Bothokakula/GLMS/actions/workflows/ci.yml/badge.svg)](https://github.com/Bothokakula/GLMS/actions/workflows/ci.yml)

An enterprise-grade web platform for **TechMove Logistics** — managing international freight contracts, driver schedules, service requests, and financial reporting with automatic currency conversion.

Built with ASP.NET Core 8, Entity Framework Core, SQL Server, and Docker.

## Architecture

```
┌─────────────────┐     HTTP      ┌─────────────────┐     EF Core     ┌──────────────┐
│   GLMS.Web      │ ──────────►   │   GLMS.API      │ ──────────────► │  SQL Server  │
│   (MVC Client)  │   HttpClient  │   (Web API)     │   Repository    │  (Azure SQL  │
│   Port 5000     │ ◄──────────   │   Port 5001     │ ◄────────────── │   Edge)      │
└─────────────────┘    JSON       └─────────────────┘                 └──────────────┘
                                         │
                                         │ HttpClient
                                         ▼
                                  ┌──────────────────┐
                                  │ ExchangeRate API │
                                  │  (USD → ZAR)     │
                                  └──────────────────┘
```

The system follows a **Service-Oriented Architecture (SOA)**:

- **GLMS.Web** — ASP.NET Core MVC frontend. Zero database dependency. Communicates with the API via `HttpClient`.
- **GLMS.API** — ASP.NET Core Web API backend. Owns all business logic, database access, and external integrations. Exposes RESTful endpoints with Swagger/OpenAPI documentation.
- **GLMS.Core** — Shared class library containing models, DTOs, interfaces, services, and design pattern implementations.
- **GLMS.Infrastructure** — Entity Framework Core data access layer with repository implementations.
- **GLMS.Tests** — xUnit test project with 49 tests (35 unit + 14 integration).

## Features

- **Contract Management** — Full CRUD with status workflow (Draft → Active → Expired / On Hold)
- **Service Request Processing** — Create requests against active contracts with automatic validation
- **Currency Conversion** — Live USD-to-ZAR exchange rates from [ExchangeRate-API](https://open.er-api.com), with caching and fallback
- **PDF File Handling** — Upload signed agreements (PDF only, UUID-named), download via UI
- **JWT Authentication** — Secure API endpoints with token-based auth
- **Search & Filter** — LINQ-powered contract filtering by status, date range, and client name
- **Automated Testing** — Unit tests (currency math, file validation, workflow rules, factory) and integration tests (API roundtrips, auth, data integrity)
- **Docker Deployment** — Full containerisation with Docker Compose (3 containers)

## Design Patterns (GoF)

| Pattern | Implementation | Purpose |
|---------|---------------|---------|
| **Factory Method** | `IContractFactory` / `ContractFactory` | Creates contracts with correct defaults based on service level |
| **Strategy** | `IValidationStrategy<T>` with 3 concrete strategies | Decouples validation logic (status transitions, file types, currency) from controllers |
| **Observer** | `IContractObserver` with `ServiceRequestStatusObserver`, `AuditLogObserver` | Cascades contract status changes to dependent service requests and audit logs |

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | ASP.NET Core 8 MVC, Razor Views, Bootstrap 5, jQuery Validation |
| Backend API | ASP.NET Core 8 Web API, Swagger/OpenAPI |
| Database | SQL Server (Azure SQL Edge for ARM Mac), Entity Framework Core 8 |
| Authentication | JWT Bearer Tokens (BCrypt password hashing) |
| Testing | xUnit, Moq, WebApplicationFactory, EF Core InMemory |
| Containerisation | Docker, Docker Compose |
| External API | [open.er-api.com](https://open.er-api.com/v6/latest/USD) (no API key required) |

## Project Structure

```
proj.net/
├── GLMS.sln
├── docker-compose.yml
├── docs/
│   ├── Part1-Architecture-Report.md      # TOGAF, design patterns, UML, scalability
│   └── Part3-Technical-Reflection.md     # CI/CD and containerisation reflection
├── src/
│   ├── GLMS.API/                         # Web API (backend)
│   │   ├── Controllers/                  # Auth, Clients, Contracts, ServiceRequests, Currency
│   │   ├── Dockerfile
│   │   └── Program.cs                    # DI, JWT, Swagger, EF Core setup
│   ├── GLMS.Core/                        # Shared library
│   │   ├── DTOs/                         # Request/Response objects
│   │   ├── Interfaces/                   # Repository, Service, Pattern interfaces
│   │   ├── Models/                       # Client, Contract, ServiceRequest, User, Enums
│   │   └── Services/                     # Factory, Strategy, Observer implementations
│   ├── GLMS.Infrastructure/              # Data access
│   │   ├── Data/GlmsDbContext.cs         # EF Core context with Fluent API + seed data
│   │   └── Repositories/                 # Contract, Client, ServiceRequest repos
│   └── GLMS.Web/                         # MVC frontend
│       ├── Controllers/                  # Home, Account, Clients, Contracts, ServiceRequests
│       ├── Dockerfile
│       ├── Services/GlmsApiClient.cs     # HttpClient wrapper for API calls
│       └── Views/                        # Razor views with Bootstrap 5
├── tests/
│   └── GLMS.Tests/
│       ├── Unit/                         # Currency, FileValidation, Workflow, Factory tests
│       └── Integration/                  # API endpoint tests with WebApplicationFactory
└── uploads/                              # Simulated file server for PDF agreements
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (only needed for Docker deployment)

### Database Connection String

The API reads the connection string in this priority order:

1. **Machine environment variable** `GLMS_CONNECTION_STRING` (if set)
2. **appsettings.json** `ConnectionStrings:DefaultConnection` (fallback — defaults to LocalDB)

The database is **auto-created on first run** via `EnsureCreated()` — no manual SQL scripts needed.

**To set the environment variable (Windows):**
```powershell
# System-level (persists across reboots, requires admin PowerShell)
[System.Environment]::SetEnvironmentVariable("GLMS_CONNECTION_STRING", "Server=(localdb)\MSSQLLocalDB;Database=GLMSDB;Trusted_Connection=True;TrustServerCertificate=True", "Machine")

# Or User-level
setx GLMS_CONNECTION_STRING "Server=(localdb)\MSSQLLocalDB;Database=GLMSDB;Trusted_Connection=True;TrustServerCertificate=True"
```

**To set the environment variable (macOS/Linux):**
```bash
export GLMS_CONNECTION_STRING="Server=localhost,1433;Database=GLMSDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
```

If no environment variable is set, the app uses the LocalDB connection string from `appsettings.json`, which works out-of-the-box on any Windows machine with SQL Server LocalDB installed (included with Visual Studio).

### Option 1: Docker Compose (recommended)

Runs everything in containers — no local SQL Server or .NET runtime needed beyond Docker.

```bash
cp .env.example .env   # then set SA_PASSWORD and JWT_KEY
docker-compose up --build
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:5000 |
| API Swagger | http://localhost:5001/swagger |

### Option 2: Local Development

```bash
# Start SQL Server in Docker
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" -p 1433:1433 -d mcr.microsoft.com/azure-sql-edge

# Build and run the API
cd src/GLMS.API
dotnet run

# In a separate terminal, run the Web frontend
cd src/GLMS.Web
dotnet run
```

### Login Credentials

| Username | Password | Role |
|----------|----------|------|
| `admin` | `Admin@123` | Admin |

## Running Tests

```bash
# Run all tests (unit + integration)
dotnet test

# Run only unit tests
dotnet test --filter "FullyQualifiedName~Unit"

# Run only integration tests
dotnet test --filter "FullyQualifiedName~Integration"
```

**Test Summary (49 tests):**

| Category | Tests | What's Covered |
|----------|-------|----------------|
| Currency Calculation | 6 | Conversion math, rounding, zero/negative amounts, API fallback |
| File Validation | 9 | PDF accepted, exe/docx/null/empty/oversized rejected, Theory with multiple types |
| Contract Workflow | 12 | Status transitions (valid + invalid), service request blocking, observer notification |
| Contract Factory | 8 | Standard/Premium defaults, title, clientId, dates, auto-correction |
| Contract API | 6 | GET list, POST create, create-then-read integrity, PATCH status, 404, invalid transition |
| Service Request API | 2 | Create on active contract (201), create on expired contract (409) |
| Auth API | 2 | Valid login returns token, invalid login returns 401 |

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/auth/login` | Authenticate and receive JWT token |
| `GET` | `/api/contracts` | List contracts (filterable by status, dates, client) |
| `GET` | `/api/contracts/{id}` | Get single contract |
| `POST` | `/api/contracts` | Create new contract |
| `PATCH` | `/api/contracts/{id}/status` | Update contract status |
| `POST` | `/api/contracts/{id}/upload` | Upload signed agreement PDF |
| `GET` | `/api/contracts/{id}/download` | Download signed agreement |
| `GET` | `/api/clients` | List all clients |
| `POST` | `/api/clients` | Create new client |
| `GET` | `/api/servicerequests?contractId={id}` | List service requests for a contract |
| `POST` | `/api/servicerequests` | Create service request (auto-converts USD to ZAR) |
| `GET` | `/api/currency/rate?from=USD&to=ZAR` | Get current exchange rate |

All endpoints except `/api/auth/login` require a Bearer token in the `Authorization` header.

## Seed Data

The database is pre-populated with realistic logistics data for immediate exploration:

- **8 clients** — Maersk, DHL, Cape Town Port Authority, Shanghai Express, Amazon SA, FedEx SA, Transnet, MSC
- **12 contracts** — Across all statuses (5 Active, 3 Draft, 2 Expired, 1 On Hold) with Standard and Premium service levels
- **14 service requests** — Freight shipments, container storage, courier services with USD/ZAR costs

## About This Project

I designed and built GLMS end to end as my Programming 3A (PROG7311) portfolio project in my final year of the Bachelor of Computer and Information Sciences (Application Development) at The IIE's Varsity College. The client, TechMove Logistics, is a fictional company from the brief. The project covers:

- **Architecture**: splitting a monolith into a service-oriented Web + API design, documented with TOGAF and UML in [`docs/`](docs/)
- **Design patterns**: Factory Method, Strategy and Observer, each solving a concrete problem in the domain
- **Security**: JWT bearer auth, BCrypt password hashing, PDF-only upload validation with UUID file names
- **Testing**: 49 xUnit tests, including integration tests with `WebApplicationFactory` and EF Core InMemory
- **DevOps**: multi-container Docker Compose deployment and GitHub Actions CI

> Seeded credentials and keys are for local demo use only.

**Author:** Botho Kakula · [GitHub](https://github.com/Bothokakula)

