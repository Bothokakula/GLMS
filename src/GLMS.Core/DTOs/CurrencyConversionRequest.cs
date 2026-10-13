// File: DTOs/CurrencyConversionRequest.cs
// Purpose: Data Transfer Object for a currency conversion to be validated before
//          execution. Consumed by CurrencyValidationStrategy (Strategy pattern)
//          to ensure the amount and exchange rate are within acceptable bounds.
// Reference: Strategy pattern — Gamma, E., Helm, R., Johnson, R. and Vlissides, J.
//            (1994) Design Patterns: Elements of Reusable Object-Oriented Software.

namespace GLMS.Core.DTOs;

/// <summary>
/// Captures the parameters of a currency conversion so they can be validated
/// by <see cref="Services.Validation.CurrencyValidationStrategy"/> before the
/// conversion is performed.
/// </summary>
public class CurrencyConversionRequest
{
    /// <summary>
    /// The monetary amount to convert. Must be non-negative.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The exchange rate to apply (e.g. USD→ZAR). Must be greater than zero
    /// to avoid division-by-zero or meaningless results.
    /// </summary>
    public decimal ExchangeRate { get; set; }
}
