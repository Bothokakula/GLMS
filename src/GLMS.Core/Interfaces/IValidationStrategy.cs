// File: Interfaces/IValidationStrategy.cs
// Purpose: Defines the contract for the Strategy pattern applied to validation.
//          Each concrete strategy encapsulates a distinct set of validation rules
//          for a specific entity type, allowing the validation algorithm to vary
//          independently of the clients that use it.
// Implementation of the Strategy pattern (Gamma et al., 1994)
// Reference: Gamma, E., Helm, R., Johnson, R. and Vlissides, J. (1994)
//            Design Patterns: Elements of Reusable Object-Oriented Software.

using GLMS.Core.DTOs;

namespace GLMS.Core.Interfaces;

/// <summary>
/// Strategy interface for validating an entity of type <typeparamref name="T"/>.
/// Concrete implementations (e.g. <see cref="Services.Validation.ContractStatusValidationStrategy"/>,
/// <see cref="Services.Validation.FileValidationStrategy"/>,
/// <see cref="Services.Validation.CurrencyValidationStrategy"/>)
/// encapsulate validation rules that can be swapped at runtime via dependency injection.
/// <para>
/// This follows the Strategy pattern (Gamma et al., 1994), where the algorithm
/// (validation logic) is extracted into its own class hierarchy so that services
/// remain open for extension (new validation rules) without modification.
/// </para>
/// </summary>
/// <typeparam name="T">The type of entity or DTO to validate (contravariant).</typeparam>
public interface IValidationStrategy<in T>
{
    /// <summary>
    /// Validates the given <paramref name="entity"/> and returns a
    /// <see cref="ValidationResult"/> containing any errors found.
    /// </summary>
    /// <param name="entity">The object to validate.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> that is valid (empty errors) when the
    /// entity passes all checks, or contains one or more error messages otherwise.
    /// </returns>
    ValidationResult Validate(T entity);
}
