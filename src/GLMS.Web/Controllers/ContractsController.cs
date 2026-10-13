// ContractsController.cs
// MVC controller for contract management: listing, viewing details, creating, status updates, and file downloads.
// Every action enforces authentication by checking for a session-stored JWT before proceeding.
// ASP.NET Core MVC pattern (Microsoft, 2024, ASP.NET Core MVC documentation)

using GLMS.Core.DTOs;
using GLMS.Core.Models;
using GLMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GLMS.Web.Controllers;

/// <summary>
/// Provides CRUD operations and workflow actions for contracts.
/// All actions require an authenticated session; unauthenticated users are redirected to the login page.
/// </summary>
public class ContractsController : Controller
{
    private readonly GlmsApiClient _apiClient;

    /// <summary>
    /// Initializes the controller with the typed API client.
    /// </summary>
    /// <param name="apiClient">The GLMS API client for contract operations.</param>
    public ContractsController(GlmsApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Checks whether the user has a valid JWT in session and sets ViewData["IsLoggedIn"]
    /// so the shared layout can render the correct navigation state.
    /// </summary>
    /// <returns>True if a JWT token exists in session; false otherwise.</returns>
    private bool IsAuthenticated()
    {
        // Session-based token storage for stateless API authentication
        var token = HttpContext.Session.GetString("JwtToken");
        ViewData["IsLoggedIn"] = !string.IsNullOrEmpty(token);
        return !string.IsNullOrEmpty(token);
    }

    /// <summary>
    /// Displays a filterable list of contracts. Supports filtering by status, date range, and client name.
    /// Redirects to login if the user is not authenticated.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="startDate">Optional start date lower bound.</param>
    /// <param name="endDate">Optional end date upper bound.</param>
    /// <param name="clientName">Optional client name substring filter.</param>
    /// <returns>The Index view with the filtered contract list.</returns>
    public async Task<IActionResult> Index(
        ContractStatus? status,
        DateTime? startDate,
        DateTime? endDate,
        string? clientName)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var contracts = await _apiClient.GetContractsAsync(status, startDate, endDate, clientName);

        // Preserve filter selections in the view so the form shows current values after postback.
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentStartDate = startDate;
        ViewBag.CurrentEndDate = endDate;
        ViewBag.CurrentClientName = clientName;
        ViewBag.Statuses = Enum.GetValues<ContractStatus>()
            .Select(s => new SelectListItem(s.ToString(), s.ToString(), s == status))
            .ToList();

        return View(contracts);
    }

    /// <summary>
    /// Displays the details of a single contract along with its associated service requests.
    /// </summary>
    /// <param name="id">The contract identifier.</param>
    /// <returns>The Details view, or NotFound if the contract does not exist.</returns>
    public async Task<IActionResult> Details(int id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var contract = await _apiClient.GetContractAsync(id);
        if (contract == null)
            return NotFound();

        var serviceRequests = await _apiClient.GetServiceRequestsAsync(id);
        ViewBag.ServiceRequests = serviceRequests;

        return View(contract);
    }

    /// <summary>
    /// Renders the contract creation form, pre-loading client and service level dropdown data.
    /// </summary>
    /// <returns>The Create view with populated dropdowns.</returns>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var clients = await _apiClient.GetClientsAsync();
        ViewBag.Clients = clients.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
        ViewBag.ServiceLevels = Enum.GetValues<ServiceLevel>()
            .Select(s => new SelectListItem(s.ToString(), s.ToString()))
            .ToList();

        return View();
    }

    /// <summary>
    /// Processes the contract creation form. Creates the contract via the API
    /// and optionally uploads a signed agreement file if one was attached.
    /// </summary>
    /// <param name="model">The contract creation form data.</param>
    /// <param name="signedAgreement">Optional signed agreement PDF upload.</param>
    /// <returns>Redirect to the contract list on success, or the form with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateContractRequest model, IFormFile? signedAgreement)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            var clients = await _apiClient.GetClientsAsync();
            ViewBag.Clients = clients.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
            ViewBag.ServiceLevels = Enum.GetValues<ServiceLevel>()
                .Select(s => new SelectListItem(s.ToString(), s.ToString()))
                .ToList();
            return View(model);
        }

        var contract = await _apiClient.CreateContractAsync(model);

        // Upload the signed agreement as a separate call only if a file was provided.
        if (signedAgreement is { Length: > 0 })
        {
            using var stream = signedAgreement.OpenReadStream();
            await _apiClient.UploadAgreementAsync(contract.Id, stream, signedAgreement.FileName, signedAgreement.ContentType);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Transitions a contract to a new status (e.g., Draft to Active).
    /// The API enforces valid state machine transitions.
    /// </summary>
    /// <param name="id">The contract identifier.</param>
    /// <param name="newStatus">The target status.</param>
    /// <returns>Redirect to the contract details page.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ContractStatus newStatus)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        await _apiClient.UpdateContractStatusAsync(id, newStatus);
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Downloads the signed agreement file for a contract.
    /// </summary>
    /// <param name="id">The contract identifier.</param>
    /// <returns>The file as a download, or NotFound if no agreement has been uploaded.</returns>
    public async Task<IActionResult> Download(int id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var result = await _apiClient.DownloadAgreementAsync(id);
        if (result == null)
            return NotFound();

        return File(result.Value.data, "application/octet-stream", result.Value.fileName);
    }
}
