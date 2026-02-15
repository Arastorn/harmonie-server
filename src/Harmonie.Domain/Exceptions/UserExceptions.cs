using Harmonie.Domain.Common;
using Harmonie.Domain.ValueObjects;

namespace Harmonie.Domain.Exceptions;

/// <summary>
/// Thrown when a user is not found
/// </summary>
public sealed class UserNotFoundException : DomainException
{
    public UserNotFoundException(UserId userId)
        : base($"User with ID '{userId}' was not found")
    {
    }

    public UserNotFoundException(Email email)
        : base($"User with email '{email}' was not found")
    {
    }

    public UserNotFoundException(Username username)
        : base($"User with username '{username}' was not found")
    {
    }
}

/// <summary>
/// Thrown when attempting to create a user with an email that already exists
/// </summary>
public sealed class DuplicateEmailException : DomainException
{
    public DuplicateEmailException(Email email)
        : base($"A user with email '{email}' already exists")
    {
    }
}

/// <summary>
/// Thrown when attempting to create a user with a username that already exists
/// </summary>
public sealed class DuplicateUsernameException : DomainException
{
    public DuplicateUsernameException(Username username)
        : base($"A user with username '{username}' already exists")
    {
    }
}

/// <summary>
/// Thrown when password validation fails
/// </summary>
public sealed class InvalidPasswordException : DomainException
{
    public InvalidPasswordException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Thrown when attempting to perform an operation on an inactive user
/// </summary>
public sealed class UserInactiveException : DomainException
{
    public UserInactiveException(UserId userId)
        : base($"User with ID '{userId}' is inactive and cannot perform this operation")
    {
    }
}
