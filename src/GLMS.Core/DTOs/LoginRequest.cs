// File: DTOs/LoginRequest.cs
// Purpose: Captures the credentials submitted by a user attempting to authenticate.
//          The password is transmitted in plain text over HTTPS and is compared
//          against the stored hash — it is never persisted.

namespace GLMS.Core.DTOs;

/// <summary>
/// Request payload for the login endpoint. Contains the user's credentials
/// which are validated against the stored <see cref="Models.User.PasswordHash"/>.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// The user's login name.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The user's plain-text password. Compared to the stored hash during
    /// authentication and never persisted.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
