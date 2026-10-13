// HomeController.cs
// Serves the landing page and static informational pages (e.g., Privacy).
// Passes authentication state to views via ViewData so the layout can adjust navigation.
// ASP.NET Core MVC pattern (Microsoft, 2024, ASP.NET Core MVC documentation)

using Microsoft.AspNetCore.Mvc;

namespace GLMS.Web.Controllers;

/// <summary>
/// Handles the application's landing page and static content pages.
/// Each action checks session state to inform the view whether the user is logged in.
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Renders the home/dashboard page. Populates IsLoggedIn for conditional navigation rendering.
    /// </summary>
    /// <returns>The Index view.</returns>
    public IActionResult Index()
    {
        // Session-based token storage for stateless API authentication
        // The layout uses this flag to toggle between "Login" and "Logout" navigation links.
        ViewData["IsLoggedIn"] = !string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken"));
        return View();
    }

    /// <summary>
    /// Renders the privacy policy page.
    /// </summary>
    /// <returns>The Privacy view.</returns>
    public IActionResult Privacy()
    {
        // Session-based token storage for stateless API authentication
        ViewData["IsLoggedIn"] = !string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken"));
        return View();
    }
}
