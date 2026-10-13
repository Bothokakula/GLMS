// AccountController.cs
// Handles user authentication (login/logout) for the GLMS web frontend.
// Stores JWT tokens in server-side session after successful API authentication.
// ASP.NET Core MVC pattern (Microsoft, 2024, ASP.NET Core MVC documentation)

using GLMS.Core.DTOs;
using GLMS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Web.Controllers;

/// <summary>
/// Manages user authentication by proxying login credentials to the GLMS API
/// and storing the returned JWT token in the server-side session.
/// </summary>
public class AccountController : Controller
{
    private readonly GlmsApiClient _apiClient;

    /// <summary>
    /// Initializes the controller with the typed API client.
    /// </summary>
    /// <param name="apiClient">The GLMS API client for authentication calls.</param>
    public AccountController(GlmsApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Displays the login page. Redirects to the home page if the user already has a valid session token.
    /// </summary>
    /// <returns>The login view, or a redirect to Home if already authenticated.</returns>
    [HttpGet]
    public IActionResult Login()
    {
        // Session-based token storage for stateless API authentication
        // Redirect already-authenticated users to avoid showing the login form unnecessarily.
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken")))
            return RedirectToAction("Index", "Home");

        return View();
    }

    /// <summary>
    /// Processes the login form submission. Authenticates against the API and stores the JWT in session on success.
    /// </summary>
    /// <param name="model">The user-submitted login credentials.</param>
    /// <returns>Redirect to Home on success, or the login view with validation errors on failure.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _apiClient.LoginAsync(model);
        if (result == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        // Session-based token storage for stateless API authentication
        // Store the JWT and username so subsequent API calls can authenticate and the UI can greet the user.
        HttpContext.Session.SetString("JwtToken", result.Token);
        HttpContext.Session.SetString("Username", result.Username);

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Logs the user out by clearing all session data, effectively invalidating the stored JWT.
    /// </summary>
    /// <returns>Redirect to the login page.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}
