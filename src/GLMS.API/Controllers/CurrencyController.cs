// ==========================================================================
// File: CurrencyController.cs
// Purpose: Exposes a currency exchange-rate lookup endpoint that delegates
//          to an external API via the ICurrencyService abstraction, allowing
//          the front end to display contract costs in multiple currencies.
// ==========================================================================

// RESTful API design (Fielding, R., 2000, Architectural Styles and the Design of Network-based Software Architectures)

using GLMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.API.Controllers;

/// <summary>
/// Provides currency exchange-rate information for the GLMS front end.
/// Requires JWT authentication.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    /// <summary>
    /// Initializes a new instance of <see cref="CurrencyController"/>.
    /// </summary>
    /// <param name="currencyService">Service that fetches live exchange rates from an external provider.</param>
    public CurrencyController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    /// <summary>
    /// Returns the current exchange rate between two currencies.
    /// </summary>
    /// <param name="from">Source currency ISO code (defaults to USD).</param>
    /// <param name="to">Target currency ISO code (defaults to ZAR).</param>
    /// <returns>
    /// 200 OK with the exchange rate;
    /// 400 Bad Request if the external service call fails (e.g., invalid currency code).
    /// </returns>
    /// <remarks>
    /// GET api/currency/rate?from=USD&amp;to=ZAR
    /// Defaults reflect the primary business use case of converting US Dollar
    /// costs to South African Rand for local reporting.
    /// </remarks>
    [HttpGet("rate")]
    public async Task<IActionResult> GetRate(
        [FromQuery] string from = "USD",
        [FromQuery] string to = "ZAR")
    {
        try
        {
            var rate = await _currencyService.GetExchangeRateAsync(from, to);
            return Ok(new { Rate = rate });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
