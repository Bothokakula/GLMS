// File: DTOs/CreateClientRequest.cs
// Purpose: Inbound DTO for registering a new client in the system. Separates the
//          API contract from the internal Client entity so the public surface can
//          evolve independently of the database schema.

namespace GLMS.Core.DTOs;

/// <summary>
/// Request payload for creating a new <see cref="Models.Client"/>.
/// All fields map directly to the corresponding entity properties; the
/// repository assigns the <c>Id</c> on persistence.
/// </summary>
public class CreateClientRequest
{
    /// <summary>
    /// Legal or trading name of the client.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact e-mail address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contact telephone number (optional).
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Geographic region the client operates in.
    /// </summary>
    public string Region { get; set; } = string.Empty;
}
