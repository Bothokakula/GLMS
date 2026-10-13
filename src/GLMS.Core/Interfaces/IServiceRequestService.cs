// File: Interfaces/IServiceRequestService.cs
// Purpose: Defines the application-level service contract for managing service
//          requests. Abstracts away currency conversion, validation, and
//          persistence so controllers depend only on this interface.
// Reference: Asynchronous programming with async and await —
//            Microsoft (2024) Asynchronous programming with async and await.

using GLMS.Core.DTOs;
using GLMS.Core.Models;

namespace GLMS.Core.Interfaces;

/// <summary>
/// Application service responsible for creating and querying
/// <see cref="ServiceRequest"/> entities. Implementations coordinate
/// between the repository, currency service, and validation strategies.
/// </summary>
public interface IServiceRequestService
{
    /// <summary>
    /// Creates a new service request, including live currency conversion
    /// from USD to ZAR and validation of the conversion parameters.
    /// </summary>
    /// <param name="request">Details of the service request to create.</param>
    /// <returns>The persisted <see cref="ServiceRequest"/> with generated Id and converted costs.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the referenced contract does not exist.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the contract is not in a status that accepts new requests,
    /// or when currency validation fails.
    /// </exception>
    Task<ServiceRequest> CreateAsync(CreateServiceRequestRequest request);

    /// <summary>
    /// Retrieves all service requests belonging to the specified contract.
    /// </summary>
    /// <param name="contractId">The contract whose requests should be returned.</param>
    /// <returns>A read-only list of service requests, possibly empty.</returns>
    Task<IReadOnlyList<ServiceRequest>> GetByContractIdAsync(int contractId);
}
