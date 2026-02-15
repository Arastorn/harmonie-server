namespace Harmonie.Domain.Common;

/// <summary>
/// Base class for all domain exceptions.
/// Domain exceptions represent violations of business rules.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
