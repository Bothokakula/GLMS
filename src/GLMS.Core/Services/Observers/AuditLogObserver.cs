// File: Services/Observers/AuditLogObserver.cs
// Purpose: Concrete Observer that writes an audit-log entry every time a contract's
//          status changes. Provides a traceable record of all lifecycle transitions
//          for compliance and debugging purposes.
// Implementation of the Observer pattern (Gamma et al., 1994)
// Reference: Gamma, E., Helm, R., Johnson, R. and Vlissides, J. (1994)
//            Design Patterns: Elements of Reusable Object-Oriented Software.

using GLMS.Core.Interfaces;
using GLMS.Core.Models;
using Microsoft.Extensions.Logging;

namespace GLMS.Core.Services.Observers;

/// <summary>
/// Observer that logs every contract status transition using the built-in
/// <see cref="ILogger"/> infrastructure. Registered as an
/// <see cref="IContractObserver"/> in the DI container alongside any other
/// observers.
/// <para>
/// This follows the Observer pattern (Gamma et al., 1994), keeping audit
/// concerns separated from the core contract logic in <see cref="ContractService"/>.
/// </para>
/// </summary>
public class AuditLogObserver : IContractObserver
{
    private readonly ILogger<AuditLogObserver> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="AuditLogObserver"/>.
    /// </summary>
    /// <param name="logger">Logger instance for writing structured audit entries.</param>
    public AuditLogObserver(ILogger<AuditLogObserver> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Logs at <see cref="LogLevel.Information"/> using structured logging
    /// placeholders so log-aggregation tools (e.g. Seq, Application Insights)
    /// can index <c>ContractId</c>, <c>OldStatus</c>, <c>NewStatus</c>, and
    /// <c>Timestamp</c> as queryable fields. Returns synchronously via
    /// <see cref="Task.CompletedTask"/> because logging is non-blocking.
    /// </remarks>
    public Task OnStatusChanged(int contractId, ContractStatus oldStatus, ContractStatus newStatus)
    {
        _logger.LogInformation(
            "Contract {ContractId} status changed from {OldStatus} to {NewStatus} at {Timestamp}",
            contractId, oldStatus, newStatus, DateTime.UtcNow);

        return Task.CompletedTask;
    }
}
