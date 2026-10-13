// File: DTOs/ClientDto.cs
// Purpose: Read-only projection of a Client entity for API responses.
//          Mirrors the entity properties but omits the Contracts navigation
//          collection to keep the payload lightweight and avoid circular references.

namespace GLMS.Core.DTOs;

/// <summary>
/// Data Transfer Object for client data exposed through the API layer.
/// Does not include the <see cref="Models.Client.Contracts"/> collection;
/// contracts are fetched via a dedicated endpoint to keep responses focused.
/// </summary>
public class ClientDto
{
    /// <summary>Unique identifier of the client.</summary>
    public int Id { get; set; }

    /// <summary>Legal or trading name of the client.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Primary contact e-mail address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Contact telephone number.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Geographic region the client operates in.</summary>
    public string Region { get; set; } = string.Empty;
}
