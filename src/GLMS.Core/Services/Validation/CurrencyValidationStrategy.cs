// File: Services/Validation/CurrencyValidationStrategy.cs
// Purpose: Concrete Strategy that validates the inputs to a currency conversion
//          (amount and exchange rate) before the conversion is performed. Prevents
//          nonsensical or dangerous values (negative amounts, zero/negative rates)
//          from reaching the calculation logic.
// Implementation of the Strategy pattern (Gamma et al., 1994)
// Reference: Gamma, E., Helm, R., Johnson, R. and Vlissides, J. (1994)
//            Design Patterns: Elements of Reusable Object-Oriented Software.

using GLMS.Core.DTOs;
using GLMS.Core.Interfaces;

namespace GLMS.Core.Services.Validation;

/// <summary>
/// Validates <see cref="CurrencyConversionRequest"/> parameters to ensure they
/// are within acceptable bounds before a conversion is executed.
/// Implements <see cref="IValidationStrategy{T}"/> for
/// <see cref="CurrencyConversionRequest"/>.
/// <para>
/// Following the Strategy pattern (Gamma et al., 1994), this class isolates
/// currency-validation rules from the <see cref="ServiceRequestService"/> that
/// consumes them, allowing the rules to be tested and replaced independently.
/// </para>
/// </summary>
public class CurrencyValidationStrategy : IValidationStrategy<CurrencyConversionRequest>
{
    /// <inheritdoc />
    /// <remarks>
    /// Enforces two constraints:
    /// 1. <see cref="CurrencyConversionRequest.Amount"/> must be ≥ 0 (zero is valid
    ///    for no-cost requests).
    /// 2. <see cref="CurrencyConversionRequest.ExchangeRate"/> must be &gt; 0 to
    ///    avoid division-by-zero or meaningless conversion results.
    /// </remarks>
    public ValidationResult Validate(CurrencyConversionRequest entity)
    {
        var result = new ValidationResult();

        if (entity.Amount < 0)
        {
            result.AddError("Amount must be greater than or equal to zero.");
        }

        if (entity.ExchangeRate <= 0)
        {
            result.AddError("Exchange rate must be greater than zero.");
        }

        return result;
    }
}
