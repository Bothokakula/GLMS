// File: Interfaces/IContractFactory.cs
// Purpose: Defines the interface for the Factory Method pattern used to create
//          Contract entities. Encapsulating construction behind a factory allows
//          the creation logic (e.g. default end-date rules) to be centralised and
//          independently testable.
// Implementation of the Factory Method pattern (Gamma et al., 1994)
// Reference: Gamma, E., Helm, R., Johnson, R. and Vlissides, J. (1994)
//            Design Patterns: Elements of Reusable Object-Oriented Software.

using GLMS.Core.Models;

namespace GLMS.Core.Interfaces;

/// <summary>
/// Factory interface for creating <see cref="Contract"/> entities.
/// Centralises creation logic so callers do not need to duplicate business
/// rules (e.g. default duration based on <see cref="ServiceLevel"/>).
/// <para>
/// This follows the Factory Method pattern (Gamma et al., 1994), allowing
/// the concrete factory to be replaced or extended without modifying callers.
/// </para>
/// </summary>
public interface IContractFactory
{
    /// <summary>
    /// Creates a new <see cref="Contract"/> in <see cref="ContractStatus.Draft"/>
    /// status, applying service-level-specific defaults when necessary.
    /// </summary>
    /// <param name="serviceLevel">The desired service tier (Standard or Premium).</param>
    /// <param name="clientId">The owning client's identifier.</param>
    /// <param name="title">Human-readable title for the contract.</param>
    /// <param name="startDate">The date the contract takes effect.</param>
    /// <param name="endDate">
    /// The requested end date. If this is on or before <paramref name="startDate"/>,
    /// the factory substitutes a default duration based on <paramref name="serviceLevel"/>.
    /// </param>
    /// <returns>A fully initialised <see cref="Contract"/> instance (not yet persisted).</returns>
    Contract CreateContract(ServiceLevel serviceLevel, int clientId, string title, DateTime startDate, DateTime endDate);
}
