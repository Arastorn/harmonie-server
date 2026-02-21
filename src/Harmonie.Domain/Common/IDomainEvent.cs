namespace Harmonie.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something important that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// When this event occurred (UTC)
    /// </summary>
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Base implementation for domain events
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
