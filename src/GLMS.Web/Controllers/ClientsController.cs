// ClientsController.cs
// MVC controller for client management: listing, viewing details, and creating new clients.
// Every action enforces session-based authentication before granting access.
// ASP.NET Core MVC pattern (Microsoft, 2024, ASP.NET Core MVC documentation)

using GLMS.Core.DTOs;
using GLMS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Web.Controllers;

/// <summary>
/// Provides CRUD operations for clients.
/// All actions require an authenticated session; unauthenticated users are redirected to the login page.
/// </summary>
public class ClientsController : Controller
{
    private readonly GlmsApiClient _apiClient;

    /// <summary>
    /// Initializes the controller with the typed API client.
    /// </summary>
    /// <param name="apiClient">The GLMS API client for client operations.</param>
    public ClientsController(GlmsApiClient apiClient)
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
    /// Displays the full list of clients. Redirects to login if the user is not authenticated.
    /// </summary>
    /// <returns>The Index view with all clients.</returns>
    public async Task<IActionResult> Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var clients = await _apiClient.GetClientsAsync();
        return View(clients);
    }

    /// <summary>
    /// Displays a single client's details along with their associated contracts.
    /// </summary>
    /// <param name="id">The client identifier.</param>
    /// <returns>The Details view, or NotFound if the client does not exist.</returns>
    public async Task<IActionResult> Details(int id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var client = await _apiClient.GetClientAsync(id);
        if (client == null)
            return NotFound();

        // Fetch contracts filtered by client name to show related contracts on the details page.
        var contracts = await _apiClient.GetContractsAsync(clientName: client.Name);
        ViewBag.Contracts = contracts;

        return View(client);
    }

    /// <summary>
    /// Renders the client creation form.
    /// </summary>
    /// <returns>The Create view.</returns>
    [HttpGet]
    public IActionResult Create()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Processes the client creation form and creates the client via the API.
    /// </summary>
    /// <param name="model">The client creation form data.</param>
    /// <returns>Redirect to the client list on success, or the form with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateClientRequest model)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
            return View(model);

        await _apiClient.CreateClientAsync(model);
        return RedirectToAction(nameof(Index));
    }
}
