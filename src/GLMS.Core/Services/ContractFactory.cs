// File: Services/ContractFactory.cs
// Purpose: Concrete implementation of the Factory Method pattern for creating
//          Contract entities. Centralises creation logic — including service-level-
//          dependent default durations — so that every call site produces contracts
//          consistently without duplicating business rules.
// Implementation of the Factory Method pattern (Gamma et al., 1994)
// Reference: Gamma, E., Helm, R., Johnson, R. and Vlissides, J. (1994)
//            Design Patterns: Elements of Reusable Object-Oriented Software.

using GLMS.Core.Interfaces;
using GLMS.Core.Models;

namespace GLMS.Core.Services;

/// <summary>
/// Factory that constructs <see cref="Contract"/> instances and applies
/// service-level-specific defaults. All new contracts start in
/// <see cref="ContractStatus.Draft"/> status.
/// <para>
/// Following the Factory Method pattern (Gamma et al., 1994), this class
/// encapsulates object-creation logic so that the <see cref="ContractService"/>
/// does not need to know about construction details or default-duration rules.
/// </para>
/// </summary>
public class ContractFactory : IContractFactory
{
    /// <summary>
    /// Creates a new <see cref="Contract"/> with the given parameters.
    /// If the requested <paramref name="endDate"/> is on or before the
    /// <paramref name="startDate"/>, a sensible default duration is applied:
    /// 2 years for <see cref="ServiceLevel.Premium"/>, 1 year for
    /// <see cref="ServiceLevel.Standard"/>.
    /// </summary>
    /// <param name="serviceLevel">Desired service tier.</param>
    /// <param name="clientId">Owning client identifier.</param>
    /// <param name="title">Human-readable contract title.</param>
    /// <param name="startDate">Effective start date.</param>
    /// <param name="endDate">Requested end date (may be overridden).</param>
    /// <returns>A fully initialised, unpersisted <see cref="Contract"/>.</returns>
    public Contract CreateContract(ServiceLevel serviceLevel, int clientId, string title, DateTime startDate, DateTime endDate)
    {
        var contract = new Contract
        {
            ClientId = clientId,
            Title = title,
            StartDate = startDate,
            EndDate = endDate,
            Status = ContractStatus.Draft,
            ServiceLevel = serviceLevel
        };

        // Premium contracts default to a 2-year term when no valid end date is provided,
        // reflecting the longer commitment expected at this tier.
        if (serviceLevel == ServiceLevel.Premium && endDate <= startDate)
        {
            contract.EndDate = startDate.AddYears(2);
        }
        // Standard contracts default to a 1-year term under the same circumstances.
        else if (serviceLevel == ServiceLevel.Standard && endDate <= startDate)
        {
            contract.EndDate = startDate.AddYears(1);
        }

        return contract;
    }
}
