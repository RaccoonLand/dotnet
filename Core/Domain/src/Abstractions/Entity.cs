namespace RaccoonLand.Core.Domain.Abstractions;

/// <summary>
/// Base class for all domain entities.
/// DDD rules implemented here:
///  1) Equality based on the key (<typeparamref name="TKey"/>).
///  2) A stable business key of type GUID that has a value before the entity is attached to the
///     database, so it can be used inside events.
///  3) Audit information (create/modify) that is populated on save by an interceptor.
/// </summary>
/// <typeparam name="TKey">The primary key type. Fully generic (Guid, int, long, ...).</typeparam>
public abstract class Entity<TKey> : IEquatable<Entity<TKey>>, IAuditable
{
    protected Entity(TKey id) => Id = id;

    /// <summary>Parameterless constructor for EF Core / materialization.</summary>
    protected Entity()
    {
    }

    /// <summary>
    /// The primary key of the entity. Init-only so it is assigned exactly once — during construction
    /// or by the ORM materializer — and never mutated afterwards. This keeps the equality/hash-code
    /// contract stable across the transient → persisted transition.
    /// </summary>
    public TKey Id { get; protected init; } = default!;

    /// <summary>
    /// Stable business key. Independent of <see cref="Id"/> and assigned at construction time
    /// (before the database allocates a key) so it can be referenced from events. Uses a version-7
    /// (sequential) GUID for better index behavior. Init-only for the same immutability reason as
    /// <see cref="Id"/>.
    /// </summary>
    public Guid BusinessKey { get; protected init; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    void IAuditable.SetCreatedAudit(DateTimeOffset occurredAtUtc, string? by)
    {
        CreatedAtUtc = occurredAtUtc;
        CreatedBy = by;
    }

    void IAuditable.SetModifiedAudit(DateTimeOffset occurredAtUtc, string? by)
    {
        ModifiedAtUtc = occurredAtUtc;
        ModifiedBy = by;
    }

    public bool Equals(Entity<TKey>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        // Transient entities (without an assigned key) are only equal by reference, not by default key.
        if (IsTransient() || other.IsTransient())
        {
            return false;
        }

        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as Entity<TKey>);

    public override int GetHashCode()
        => IsTransient() ? base.GetHashCode() : HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right) => !(left == right);

    private bool IsTransient() => EqualityComparer<TKey>.Default.Equals(Id, default!);
}
