// ==========================================================================
// File: ClientsController.cs
// Purpose: Exposes RESTful endpoints for managing logistics clients,
//          including listing all clients, retrieving a single client by ID,
//          and creating new client records.
// ==========================================================================

// RESTful API design (Fielding, R., 2000, Architectural Styles and the Design of Network-based Software Architectures)
// Repository pattern implementation (Fowler, M., 2002, Patterns of Enterprise Application Architecture)

using GLMS.Core.DTOs;
using GLMS.Core.Interfaces;
using GLMS.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.API.Controllers;

/// <summary>
/// RESTful controller for client CRUD operations.
/// All endpoints require a valid JWT Bearer token (the <see cref="AuthorizeAttribute"/>
/// is applied at the controller level).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IClientRepository _clientRepository;

    /// <summary>
    /// Initializes a new instance of <see cref="ClientsController"/>.
    /// </summary>
    /// <param name="clientRepository">Repository abstraction for client data access.</param>
    public ClientsController(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    /// <summary>
    /// Retrieves all clients in the system.
    /// </summary>
    /// <returns>
    /// 200 OK with a list of <see cref="ClientDto"/> objects.
    /// </returns>
    /// <remarks>
    /// GET api/clients
    /// Maps domain models to DTOs to avoid exposing internal entity structure
    /// and to control the shape of the response payload.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clients = await _clientRepository.GetAllAsync();

        // Project domain entities to DTOs, decoupling the API contract from the persistence model.
        var dtos = clients.Select(c => new ClientDto
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Phone = c.Phone,
            Region = c.Region
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Retrieves a single client by its unique identifier.
    /// </summary>
    /// <param name="id">The client's primary key.</param>
    /// <returns>
    /// 200 OK with a <see cref="ClientDto"/> if found;
    /// 404 Not Found if no client exists with the given ID.
    /// </returns>
    /// <remarks>
    /// GET api/clients/{id}
    /// </remarks>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var client = await _clientRepository.GetByIdAsync(id);
        if (client is null)
            return NotFound();

        var dto = new ClientDto
        {
            Id = client.Id,
            Name = client.Name,
            Email = client.Email,
            Phone = client.Phone,
            Region = client.Region
        };

        return Ok(dto);
    }

    /// <summary>
    /// Creates a new client record.
    /// </summary>
    /// <param name="request">Client details supplied in the request body.</param>
    /// <returns>
    /// 201 Created with the new <see cref="ClientDto"/> and a Location header
    /// pointing to the newly created resource.
    /// </returns>
    /// <remarks>
    /// POST api/clients
    /// Returns a 201 with a Location header per REST conventions, allowing the
    /// consumer to follow the URI to the newly created resource.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request)
    {
        var client = new Client
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Region = request.Region
        };

        var created = await _clientRepository.AddAsync(client);

        var dto = new ClientDto
        {
            Id = created.Id,
            Name = created.Name,
            Email = created.Email,
            Phone = created.Phone,
            Region = created.Region
        };

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, dto);
    }
}
