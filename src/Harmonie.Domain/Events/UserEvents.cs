using Harmonie.Domain.Common;
using Harmonie.Domain.ValueObjects;

namespace Harmonie.Domain.Events;

/// <summary>
/// Raised when a new user is created
/// </summary>
public sealed record UserCreatedEvent(
    UserId UserId,
    Email Email,
    Username Username) : DomainEvent;

/// <summary>
/// Raised when a user's email address changes
/// </summary>
public sealed record UserEmailChangedEvent(
    UserId UserId,
    Email OldEmail,
    Email NewEmail) : DomainEvent;

/// <summary>
/// Raised when a user's username changes
/// </summary>
public sealed record UserUsernameChangedEvent(
    UserId UserId,
    Username OldUsername,
    Username NewUsername) : DomainEvent;

/// <summary>
/// Raised when a user's password is changed
/// </summary>
public sealed record UserPasswordChangedEvent(
    UserId UserId) : DomainEvent;

/// <summary>
/// Raised when a user verifies their email address
/// </summary>
public sealed record UserEmailVerifiedEvent(
    UserId UserId,
    Email Email) : DomainEvent;

/// <summary>
/// Raised when a user account is deactivated
/// </summary>
public sealed record UserDeactivatedEvent(
    UserId UserId) : DomainEvent;

/// <summary>
/// Raised when a user account is reactivated
/// </summary>
public sealed record UserReactivatedEvent(
    UserId UserId) : DomainEvent;
