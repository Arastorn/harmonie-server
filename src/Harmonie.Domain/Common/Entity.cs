namespace Harmonie.Domain.Common;

/// <summary>
/// Base class for all domain entities with identity and domain events support.
/// </summary>
/// <typeparam name="TId">Type of the entity identifier (strongly-typed ID)</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Unique identifier for this entity
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// When this entity was created (UTC)
    /// </summary>
    public DateTime CreatedAtUtc { get; protected set; }

    /// <summary>
    /// When this entity was last updated (UTC)
    /// </summary>
    public DateTime? UpdatedAtUtc { get; protected set; }

    /// <summary>
    /// Read-only collection of domain events raised by this entity
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Add a domain event to be published after persistence
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clear all domain events (called after they've been published)
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Mark entity as updated
    /// </summary>
    protected void MarkAsUpdated()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    #region Equality

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }

    #endregion
}
