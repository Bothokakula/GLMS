// ==========================================================================
// File: AuthController.cs
// Purpose: Handles user authentication by validating credentials against
//          the database and issuing signed JWT tokens for subsequent
//          authorized API access.
// ==========================================================================

// JWT Bearer authentication (Microsoft, 2024, ASP.NET Core Security documentation)
// RESTful API design (Fielding, R., 2000, Architectural Styles and the Design of Network-based Software Architectures)

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GLMS.Core.DTOs;
using GLMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GLMS.API.Controllers;

/// <summary>
/// Provides the authentication endpoint for the GLMS API.
/// Issues JWT tokens upon successful credential verification, enabling
/// stateless authentication across all protected endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly GlmsDbContext _dbContext;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthController"/>.
    /// </summary>
    /// <param name="dbContext">Database context for querying user credentials.</param>
    /// <param name="configuration">Application configuration for reading JWT settings.</param>
    public AuthController(GlmsDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    /// <summary>
    /// Authenticates a user and returns a signed JWT token.
    /// </summary>
    /// <param name="request">Login credentials containing username and password.</param>
    /// <returns>
    /// 200 OK with a <see cref="LoginResponse"/> containing the JWT token, username, and expiration;
    /// 401 Unauthorized if the username does not exist or the password is incorrect.
    /// </returns>
    /// <remarks>
    /// POST api/auth/login
    /// The password is verified against a BCrypt hash stored in the database,
    /// so plaintext passwords are never persisted.
    /// </remarks>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Query the database for a user matching the supplied username.
        // FirstOrDefaultAsync returns null if no match is found.
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null)
            return Unauthorized(new { error = "Invalid username or password." });

        // BCrypt.Verify compares the plaintext password against the stored hash
        // using the salt embedded in the hash itself, preventing timing attacks.
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid username or password." });

        // JWT Bearer authentication (Microsoft, 2024, ASP.NET Core Security documentation)
        // Read signing parameters from configuration — these must match the values
        // used in Program.cs for token validation to succeed.
        var key = _configuration["Jwt:Key"]!;
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Embed identity claims in the token so downstream middleware can extract
        // the user's name, role, and unique identifier without a database round-trip.
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiration = DateTime.UtcNow.AddHours(24);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse
        {
            Token = tokenString,
            Username = user.Username,
            Expiration = expiration
        });
    }
}
