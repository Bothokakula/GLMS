// File: DTOs/ValidationResult.cs
// Purpose: Provides a lightweight, domain-level validation result that aggregates
//          zero or more error messages. Used by all IValidationStrategy implementations
//          to return validation outcomes without throwing exceptions, enabling callers
//          to inspect errors and decide how to respond.

namespace GLMS.Core.DTOs;

/// <summary>
/// Encapsulates the outcome of a validation operation. Collects error messages
/// and exposes a convenience <see cref="IsValid"/> flag so callers can branch
/// without inspecting the error list directly.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation passed (i.e. no errors were recorded).
    /// Derived from <see cref="Errors"/> count to ensure the two are always consistent.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Accumulated validation error messages. Empty when the entity is valid.
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// Records a single validation error. Multiple errors can be added so that
    /// the caller receives all problems in one pass rather than fixing them one at a time.
    /// </summary>
    /// <param name="error">A human-readable description of the validation failure.</param>
    public void AddError(string error)
    {
        Errors.Add(error);
    }

    /// <summary>
    /// Factory method that creates a successful (error-free) result.
    /// Provided for readability at call sites that need an explicit success value.
    /// </summary>
    /// <returns>A new <see cref="ValidationResult"/> with no errors.</returns>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Factory method that creates a failed result containing a single error.
    /// Useful for quick early-return validation checks.
    /// </summary>
    /// <param name="error">A human-readable description of the validation failure.</param>
    /// <returns>A new <see cref="ValidationResult"/> pre-populated with one error.</returns>
    public static ValidationResult Failure(string error)
    {
        var result = new ValidationResult();
        result.AddError(error);
        return result;
    }
}
