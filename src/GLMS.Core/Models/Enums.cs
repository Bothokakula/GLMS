// File: Models/Enums.cs
// Purpose: Centralises the enumeration types used throughout the GLMS domain layer.
//          Keeping enums in a single file avoids circular references and makes it easy
//          for both the domain models and the service layer to share the same vocabulary.

namespace GLMS.Core.Models;

/// <summary>
/// Represents the lifecycle status of a <see cref="Contract"/>.
/// Valid transitions are: Draft → Active → Expired | OnHold, and OnHold → Active.
/// These transitions are enforced by
/// <see cref="Services.Validation.ContractStatusValidationStrategy"/>.
/// </summary>
public enum ContractStatus
{
    /// <summary>Contract has been created but not yet activated.</summary>
    Draft,

    /// <summary>Contract is currently in force and can accept service requests.</summary>
    Active,

    /// <summary>Contract has passed its validity period; no further work is allowed.</summary>
    Expired,

    /// <summary>Contract is temporarily suspended; existing requests are paused.</summary>
    OnHold
}

/// <summary>
/// Defines the tier of service associated with a <see cref="Contract"/>.
/// The service level influences business rules such as default contract duration
/// (see <see cref="Services.ContractFactory"/>).
/// </summary>
public enum ServiceLevel
{
    /// <summary>Standard-tier service with a default duration of 1 year.</summary>
    Standard,

    /// <summary>Premium-tier service with a default duration of 2 years.</summary>
    Premium
}
