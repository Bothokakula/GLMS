// File: DTOs/ContractFilter.cs
// Purpose: Encapsulates optional query parameters for filtering contracts.
//          By bundling filter criteria into a single DTO, the repository method
//          signature remains stable even as new filters are added in the future.

using GLMS.Core.Models;

namespace GLMS.Core.DTOs;

/// <summary>
/// Optional filter criteria applied when retrieving a list of contracts.
/// All properties are nullable — a null value means "do not filter on this field".
/// </summary>
public class ContractFilter
{
    /// <summary>
    /// When set, only contracts with this lifecycle status are returned.
    /// </summary>
    public ContractStatus? Status { get; set; }

    /// <summary>
    /// When set, only contracts whose <see cref="Contract.StartDate"/> is on
    /// or after this date are included.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// When set, only contracts whose <see cref="Contract.EndDate"/> is on
    /// or before this date are included.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// When set, only contracts belonging to a client whose name contains
    /// this substring (case-insensitive) are included.
    /// </summary>
    public string? ClientName { get; set; }
}
