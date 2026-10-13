// File: Interfaces/ICurrencyService.cs
// Purpose: Abstracts currency exchange-rate retrieval and conversion. Allows the
//          service layer to work with exchange rates without knowing whether they
//          come from a live API, a cache, or a test stub.
// Reference: Asynchronous programming with async and await —
//            Microsoft (2024) Asynchronous programming with async and await.

namespace GLMS.Core.Interfaces;

/// <summary>
/// Service contract for fetching live exchange rates and performing currency
/// conversions. The concrete implementation (<see cref="Services.CurrencyService"/>)
/// uses <c>HttpClient</c> with in-memory caching and stale-data fallback.
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Retrieves the current exchange rate between two currencies.
    /// Implementations may cache the result for a short period to reduce
    /// external API calls.
    /// </summary>
    /// <param name="from">ISO 4217 source currency code (e.g. "USD").</param>
    /// <param name="to">ISO 4217 target currency code (e.g. "ZAR").</param>
    /// <returns>The exchange rate (target units per 1 source unit).</returns>
    Task<decimal> GetExchangeRateAsync(string from, string to);

    /// <summary>
    /// Converts a monetary <paramref name="amount"/> using the given
    /// <paramref name="rate"/>, rounding to 2 decimal places.
    /// </summary>
    /// <param name="amount">The amount in the source currency.</param>
    /// <param name="rate">The exchange rate to apply.</param>
    /// <returns>The converted amount, rounded to 2 decimal places.</returns>
    decimal ConvertCurrency(decimal amount, decimal rate);
}
