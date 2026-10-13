// ==========================================================================
// File: GlmsDbContext.cs
// Purpose: Entity Framework Core database context for the GLMS application.
//          Defines DbSets for all domain entities, configures the relational
//          schema via the Fluent API, and seeds initial reference data so the
//          application is usable immediately after database creation.
// ==========================================================================

// Entity Framework Core data access (Microsoft, 2024, EF Core documentation)

using GLMS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Infrastructure.Data;

/// <summary>
/// Central EF Core database context that maps domain models to SQL Server tables
/// and configures relationships, constraints, and seed data via the Fluent API.
/// </summary>
public class GlmsDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="GlmsDbContext"/> with the specified options.
    /// </summary>
    /// <param name="options">
    /// Configuration options (e.g., connection string, provider) injected by the DI container.
    /// </param>
    public GlmsDbContext(DbContextOptions<GlmsDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets or sets the <see cref="Client"/> table.</summary>
    public DbSet<Client> Clients => Set<Client>();

    /// <summary>Gets or sets the <see cref="Contract"/> table.</summary>
    public DbSet<Contract> Contracts => Set<Contract>();

    /// <summary>Gets or sets the <see cref="ServiceRequest"/> table.</summary>
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    /// <summary>Gets or sets the <see cref="User"/> table used for authentication.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Configures the database schema using the Fluent API, defining column constraints,
    /// relationships, and seed data. Called once during model creation.
    /// </summary>
    /// <param name="modelBuilder">Builder used to construct the EF Core model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // -----------------------------------------------------------------
        // Client entity configuration
        // -----------------------------------------------------------------
        modelBuilder.Entity<Client>(entity =>
        {
            // Name is mandatory and capped at 200 chars to align with UI display limits.
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            // Email is mandatory; uniqueness is enforced at the application layer.
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(200);
            // Region is optional but constrained to 100 chars for consistency.
            entity.Property(e => e.Region)
                .HasMaxLength(100);
        });

        // -----------------------------------------------------------------
        // Contract entity configuration
        // -----------------------------------------------------------------
        modelBuilder.Entity<Contract>(entity =>
        {
            // Title is mandatory with a generous 300-char limit for descriptive names.
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(300);
            // Store enum values as their string names rather than integers so the
            // database remains human-readable and resilient to enum reordering.
            entity.Property(e => e.Status)
                .HasConversion<string>();
            entity.Property(e => e.ServiceLevel)
                .HasConversion<string>();
            // Path to the uploaded signed-agreement file on the server file system.
            entity.Property(e => e.SignedAgreementPath)
                .HasMaxLength(500);

            // Each contract belongs to exactly one client; deleting a client
            // cascades to remove all associated contracts.
            entity.HasOne(e => e.Client)
                .WithMany(c => c.Contracts)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // -----------------------------------------------------------------
        // ServiceRequest entity configuration
        // -----------------------------------------------------------------
        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            // Description is required with a 1000-char cap for detailed request notes.
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(1000);
            // Monetary values use precision(18,2) to match standard accounting conventions,
            // avoiding floating-point rounding errors in financial calculations.
            entity.Property(e => e.CostUSD)
                .HasPrecision(18, 2);
            entity.Property(e => e.CostZAR)
                .HasPrecision(18, 2);
            entity.Property(e => e.ExchangeRate)
                .HasPrecision(18, 2);
            // Status stored as a string (e.g., "Pending", "Active") for readability.
            entity.Property(e => e.Status)
                .HasMaxLength(50);

            // Each service request belongs to one contract; deleting a contract
            // cascades to remove its service requests.
            entity.HasOne(e => e.Contract)
                .WithMany(c => c.ServiceRequests)
                .HasForeignKey(e => e.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // -----------------------------------------------------------------
        // User entity configuration
        // -----------------------------------------------------------------
        modelBuilder.Entity<User>(entity =>
        {
            // Username is required and limited to 100 chars.
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);
            // PasswordHash stores the BCrypt-hashed password; length is unconstrained
            // because BCrypt output varies.
            entity.Property(e => e.PasswordHash)
                .IsRequired();
            // Role determines authorization scope (e.g., "Admin").
            entity.Property(e => e.Role)
                .HasMaxLength(50);
        });

        // =================================================================
        // Seed Data
        // Provides a default admin user and representative sample data so
        // the application is immediately usable for demonstration purposes.
        // =================================================================

        // Default admin account (password: "Admin@123") — BCrypt-hashed so the
        // plaintext password is never stored in the database.
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = "$2a$11$2SdCpkfbw.M.IpNXU2byKudxdYJZAWoM2JhnPQiDnMi2yg2JWMUIq",
            Role = "Admin"
        });

        // Seed clients representing a mix of global and regional logistics companies.
        modelBuilder.Entity<Client>().HasData(
            new Client { Id = 1, Name = "Maersk Shipping Co.", Email = "logistics@maersk.com", Phone = "+45 3363 3363", Region = "Europe" },
            new Client { Id = 2, Name = "DHL Global Forwarding", Email = "contracts@dhl.com", Phone = "+49 228 1820", Region = "Europe" },
            new Client { Id = 3, Name = "Cape Town Port Authority", Email = "operations@ctpa.co.za", Phone = "+27 21 449 3911", Region = "Africa" },
            new Client { Id = 4, Name = "Shanghai Express Freight", Email = "info@shanghaiexpress.cn", Phone = "+86 21 5888 0000", Region = "Asia" },
            new Client { Id = 5, Name = "Amazon Logistics SA", Email = "freight@amazon.co.za", Phone = "+27 11 234 5678", Region = "Africa" },
            new Client { Id = 6, Name = "FedEx South Africa", Email = "corporate@fedex.co.za", Phone = "+27 11 923 8000", Region = "Africa" },
            new Client { Id = 7, Name = "Transnet SOC Ltd", Email = "contracts@transnet.net", Phone = "+27 11 308 2911", Region = "Africa" },
            new Client { Id = 8, Name = "Mediterranean Shipping Co.", Email = "bookings@msc.com", Phone = "+41 22 703 8888", Region = "Europe" }
        );

        // Seed contracts covering various statuses (Active, Draft, Expired, OnHold)
        // and service levels (Premium, Standard) to demonstrate filtering.
        modelBuilder.Entity<Contract>().HasData(
            new Contract { Id = 1, ClientId = 1, Title = "Trans-Atlantic Freight Agreement 2026", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2027, 12, 31), Status = ContractStatus.Active, ServiceLevel = ServiceLevel.Premium },
            new Contract { Id = 2, ClientId = 1, Title = "Baltic Sea Route Contract", StartDate = new DateTime(2025, 6, 1), EndDate = new DateTime(2026, 5, 31), Status = ContractStatus.Active, ServiceLevel = ServiceLevel.Standard },
            new Contract { Id = 3, ClientId = 2, Title = "DHL Express Courier Partnership", StartDate = new DateTime(2026, 3, 1), EndDate = new DateTime(2028, 2, 28), Status = ContractStatus.Draft, ServiceLevel = ServiceLevel.Premium },
            new Contract { Id = 4, ClientId = 3, Title = "Cape Town Harbour Docking Rights", StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 12, 31), Status = ContractStatus.Expired, ServiceLevel = ServiceLevel.Standard },
            new Contract { Id = 5, ClientId = 3, Title = "Cape Town Harbour Docking Rights (Renewed)", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2027, 12, 31), Status = ContractStatus.Active, ServiceLevel = ServiceLevel.Premium },
            new Contract { Id = 6, ClientId = 4, Title = "Shanghai-Durban Shipping Lane", StartDate = new DateTime(2026, 2, 15), EndDate = new DateTime(2027, 2, 14), Status = ContractStatus.Active, ServiceLevel = ServiceLevel.Standard },
            new Contract { Id = 7, ClientId = 5, Title = "Amazon Warehouse Distribution SA", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31), Status = ContractStatus.OnHold, ServiceLevel = ServiceLevel.Premium },
            new Contract { Id = 8, ClientId = 6, Title = "FedEx Ground Freight Partnership", StartDate = new DateTime(2026, 4, 1), EndDate = new DateTime(2028, 3, 31), Status = ContractStatus.Draft, ServiceLevel = ServiceLevel.Standard },
            new Contract { Id = 9, ClientId = 7, Title = "Transnet Rail Freight Integration", StartDate = new DateTime(2025, 7, 1), EndDate = new DateTime(2027, 6, 30), Status = ContractStatus.Active, ServiceLevel = ServiceLevel.Premium },
            new Contract { Id = 10, ClientId = 8, Title = "MSC Container Leasing Agreement", StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2025, 6, 30), Status = ContractStatus.Expired, ServiceLevel = ServiceLevel.Standard },
            new Contract { Id = 11, ClientId = 2, Title = "DHL Air Freight - Johannesburg Hub", StartDate = new DateTime(2026, 1, 15), EndDate = new DateTime(2027, 1, 14), Status = ContractStatus.Active, ServiceLevel = ServiceLevel.Standard },
            new Contract { Id = 12, ClientId = 4, Title = "Shanghai Bulk Cargo Contract", StartDate = new DateTime(2026, 5, 1), EndDate = new DateTime(2027, 4, 30), Status = ContractStatus.Draft, ServiceLevel = ServiceLevel.Premium }
        );

        // Seed service requests spread across multiple contracts to demonstrate
        // the parent-child relationship and multi-currency cost tracking.
        modelBuilder.Entity<ServiceRequest>().HasData(
            new ServiceRequest { Id = 1, ContractId = 1, Description = "Ship 40ft container from Rotterdam to New York", CostUSD = 4500.00m, CostZAR = 83250.00m, ExchangeRate = 18.50m, Status = "Pending", CreatedDate = new DateTime(2026, 2, 10) },
            new ServiceRequest { Id = 2, ContractId = 1, Description = "Refrigerated cargo transport - Hamburg to Boston", CostUSD = 8200.00m, CostZAR = 151700.00m, ExchangeRate = 18.50m, Status = "Active", CreatedDate = new DateTime(2026, 2, 15) },
            new ServiceRequest { Id = 3, ContractId = 1, Description = "Hazardous materials shipment - Antwerp to Newark", CostUSD = 12000.00m, CostZAR = 222000.00m, ExchangeRate = 18.50m, Status = "Pending", CreatedDate = new DateTime(2026, 3, 1) },
            new ServiceRequest { Id = 4, ContractId = 2, Description = "Bulk grain transport - Copenhagen to Gdansk", CostUSD = 3200.00m, CostZAR = 59200.00m, ExchangeRate = 18.50m, Status = "Active", CreatedDate = new DateTime(2026, 1, 20) },
            new ServiceRequest { Id = 5, ContractId = 5, Description = "Vehicle import clearance - Cape Town port", CostUSD = 1500.00m, CostZAR = 27750.00m, ExchangeRate = 18.50m, Status = "Pending", CreatedDate = new DateTime(2026, 2, 28) },
            new ServiceRequest { Id = 6, ContractId = 5, Description = "Container storage extension - 30 days", CostUSD = 750.00m, CostZAR = 13875.00m, ExchangeRate = 18.50m, Status = "Active", CreatedDate = new DateTime(2026, 3, 5) },
            new ServiceRequest { Id = 7, ContractId = 6, Description = "Electronics shipment - Shanghai to Durban", CostUSD = 6800.00m, CostZAR = 125800.00m, ExchangeRate = 18.50m, Status = "Pending", CreatedDate = new DateTime(2026, 3, 2) },
            new ServiceRequest { Id = 8, ContractId = 6, Description = "Textile cargo - Shanghai to Cape Town", CostUSD = 3400.00m, CostZAR = 62900.00m, ExchangeRate = 18.50m, Status = "Active", CreatedDate = new DateTime(2026, 2, 20) },
            new ServiceRequest { Id = 9, ContractId = 9, Description = "Coal transport - Richards Bay to Johannesburg", CostUSD = 2100.00m, CostZAR = 38850.00m, ExchangeRate = 18.50m, Status = "Active", CreatedDate = new DateTime(2026, 1, 15) },
            new ServiceRequest { Id = 10, ContractId = 9, Description = "Mining equipment rail freight - Pretoria to Durban", CostUSD = 5500.00m, CostZAR = 101750.00m, ExchangeRate = 18.50m, Status = "Pending", CreatedDate = new DateTime(2026, 2, 25) },
            new ServiceRequest { Id = 11, ContractId = 11, Description = "Express parcel delivery - JHB to London", CostUSD = 850.00m, CostZAR = 15725.00m, ExchangeRate = 18.50m, Status = "Active", CreatedDate = new DateTime(2026, 2, 5) },
            new ServiceRequest { Id = 12, ContractId = 11, Description = "Document courier - Durban to Frankfurt", CostUSD = 320.00m, CostZAR = 5920.00m, ExchangeRate = 18.50m, Status = "Pending", CreatedDate = new DateTime(2026, 3, 3) },
            new ServiceRequest { Id = 13, ContractId = 2, Description = "Timber cargo - Helsinki to Stockholm", CostUSD = 2800.00m, CostZAR = 51800.00m, ExchangeRate = 18.50m, Status = "Pending", CreatedDate = new DateTime(2026, 3, 6) },
            new ServiceRequest { Id = 14, ContractId = 9, Description = "Automotive parts - East London to Gauteng", CostUSD = 1800.00m, CostZAR = 33300.00m, ExchangeRate = 18.50m, Status = "Active", CreatedDate = new DateTime(2026, 1, 30) }
        );
    }
}
