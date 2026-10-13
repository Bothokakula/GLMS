// File: DTOs/LoginResponse.cs
// Purpose: Returned to the client after successful authentication. Contains
//          the JWT bearer token, the authenticated username, and the token's
//          expiration time so the client can proactively refresh before expiry.

namespace GLMS.Core.DTOs;

/// <summary>
/// Response payload issued upon successful user authentication.
/// The client should store the <see cref="Token"/> and include it in the
/// Authorization header of subsequent API requests.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT bearer token to be sent in the <c>Authorization: Bearer {token}</c> header.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Username of the authenticated user, echoed back for UI display purposes.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// UTC date/time at which the token expires. Enables the client to schedule
    /// a token refresh before the current token becomes invalid.
    /// </summary>
    public DateTime Expiration { get; set; }
}
