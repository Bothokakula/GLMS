// ServiceRequestsController.cs
// MVC controller for service request management under contracts.
// Displays live USD-to-ZAR exchange rates on the creation form for cost preview.
// ASP.NET Core MVC pattern (Microsoft, 2024, ASP.NET Core MVC documentation)

using GLMS.Core.DTOs;
using GLMS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Web.Controllers;

/// <summary>
/// Manages service request operations: listing requests for a contract and creating new ones.
/// All actions require an authenticated session; unauthenticated users are redirected to the login page.
/// </summary>
public class ServiceRequestsController : Controller
{
    private readonly GlmsApiClient _apiClient;

    /// <summary>
    /// Initializes the controller with the typed API client.
    /// </summary>
    /// <param name="apiClient">The GLMS API client for service request and currency operations.</param>
    public ServiceRequestsController(GlmsApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Checks whether the user has a valid JWT in session and sets ViewData["IsLoggedIn"]
    /// so the shared layout can render the correct navigation state.
    /// </summary>
    /// <returns>True if authenticated; false otherwise.</returns>
    private bool IsAuthenticated()
    {
        // Session-based token storage for stateless API authentication
        var token = HttpContext.Session.GetString("JwtToken");
        ViewData["IsLoggedIn"] = !string.IsNullOrEmpty(token);
        return !string.IsNullOrEmpty(token);
    }

    /// <summary>
    /// Displays all service requests for a given contract.
    /// </summary>
    /// <param name="contractId">The parent contract identifier.</param>
    /// <returns>The Index view with the list of service requests.</returns>
    public async Task<IActionResult> Index(int contractId)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var serviceRequests = await _apiClient.GetServiceRequestsAsync(contractId);
        ViewBag.ContractId = contractId;

        return View(serviceRequests);
    }

    /// <summary>
    /// Renders the service request creation form with a live exchange rate for cost preview.
    /// Falls back to a default rate of 18.0 ZAR/USD if the currency API is unavailable,
    /// ensuring the form remains usable even during external service outages.
    /// </summary>
    /// <param name="contractId">The parent contract identifier.</param>
    /// <returns>The Create view with exchange rate data.</returns>
    [HttpGet]
    public async Task<IActionResult> Create(int contractId)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        decimal rate = 18.0m;
        try
        {
            rate = await _apiClient.GetExchangeRateAsync("USD", "ZAR");
        }
        catch
        {
            // Fall back to default rate if API is unavailable
        }

        ViewBag.ContractId = contractId;
        ViewBag.ExchangeRate = rate;

        return View();
    }

    /// <summary>
    /// Processes the service request creation form. Creates the request via the API,
    /// which performs server-side currency conversion. On validation failure, re-fetches
    /// the exchange rate so the form can re-render with an up-to-date cost preview.
    /// </summary>
    /// <param name="model">The service request creation form data.</param>
    /// <returns>Redirect to the service request list on success, or the form with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateServiceRequestRequest model)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            decimal rate = 18.0m;
            try
            {
                rate = await _apiClient.GetExchangeRateAsync("USD", "ZAR");
            }
            catch { }

            ViewBag.ContractId = model.ContractId;
            ViewBag.ExchangeRate = rate;
            return View(model);
        }

        await _apiClient.CreateServiceRequestAsync(model);
        return RedirectToAction(nameof(Index), new { contractId = model.ContractId });
    }
}
