// File: Services/CurrencyService.cs
// Purpose: Retrieves live exchange rates from an external REST API and performs
//          currency conversions. Implements a short-lived in-memory cache to reduce
//          the number of outbound HTTP calls, and falls back to stale cache data
//          (or a hard-coded default for USD→ZAR) when the API is unreachable.
// References:
//   - HttpClient usage via IHttpClientFactory — Microsoft (2024) Make HTTP requests
//     using IHttpClientFactory in ASP.NET Core.
//   - Asynchronous programming with async and await —
//     Microsoft (2024) Asynchronous programming with async and await.

using System.Net.Http.Json;
using System.Text.Json;
using GLMS.Core.Interfaces;

namespace GLMS.Core.Services;

/// <summary>
/// Concrete <see cref="ICurrencyService"/> implementation that fetches exchange
/// rates from the open.er-api.com public API. The <see cref="HttpClient"/> is
/// expected to be injected via <c>IHttpClientFactory</c> to benefit from
/// connection pooling and DNS rotation, as recommended by Microsoft (2024).
/// </summary>
public class CurrencyService : ICurrencyService
{
    private readonly HttpClient _httpClient;

    // In-memory cache keyed by "FROM_TO" (e.g. "USD_ZAR"). Each entry stores
    // the rate and the UTC timestamp when it was fetched so we can honour the TTL.
    private readonly Dictionary<string, (decimal Rate, DateTime FetchedAt)> _cache = new();

    // Rates are cached for 5 minutes to balance freshness against API rate limits.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initialises a new instance of <see cref="CurrencyService"/>.
    /// </summary>
    /// <param name="httpClient">
    /// An <see cref="HttpClient"/> instance, ideally provided by
    /// <c>IHttpClientFactory</c> to manage connection lifetimes correctly
    /// (Microsoft, 2024 — Make HTTP requests using IHttpClientFactory in ASP.NET Core).
    /// </param>
    public CurrencyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Uses a simple cache-aside strategy: if a non-expired entry exists, it is
    /// returned immediately without an HTTP call. Otherwise the external API is
    /// queried and the result is stored.
    /// </para>
    /// <para>
    /// On HTTP failure the method degrades gracefully:
    /// 1. Returns stale cached data if available.
    /// 2. Falls back to a hard-coded 18.5 for the USD→ZAR pair (a common default
    ///    for the South African market).
    /// 3. Re-throws the exception for any other currency pair.
    /// </para>
    /// </remarks>
    public async Task<decimal> GetExchangeRateAsync(string from, string to)
    {
        var cacheKey = $"{from}_{to}";

        // Return cached rate if still within the TTL window.
        if (_cache.TryGetValue(cacheKey, out var cached) &&
            DateTime.UtcNow - cached.FetchedAt < CacheTtl)
        {
            return cached.Rate;
        }

        try
        {
            var response = await _httpClient.GetAsync($"https://open.er-api.com/v6/latest/{from}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var rates = json.GetProperty("rates");

            if (!rates.TryGetProperty(to, out var rateElement))
            {
                throw new InvalidOperationException($"Exchange rate for {to} not found in API response.");
            }

            var rate = rateElement.GetDecimal();
            _cache[cacheKey] = (rate, DateTime.UtcNow);

            return rate;
        }
        catch (HttpRequestException)
        {
            // Graceful degradation: prefer stale data over failing the request entirely.
            if (_cache.TryGetValue(cacheKey, out var staleCache))
            {
                return staleCache.Rate;
            }

            // Hard-coded fallback for the primary conversion pair used in this system.
            if (from == "USD" && to == "ZAR")
                return 18.5m;

            throw;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Rounds to 2 decimal places using banker's rounding (default for
    /// <see cref="Math.Round(decimal, int)"/>) to minimise cumulative rounding errors.
    /// </remarks>
    public decimal ConvertCurrency(decimal amount, decimal rate)
    {
        return Math.Round(amount * rate, 2);
    }
}
