namespace RaccoonLand.Core.Domain.Abstractions;

/// <summary>
/// Audit-population contract. The mutating methods are implemented explicitly so that
/// only the infrastructure layer (interceptors) can set the values during save, keeping
/// these details out of the domain-facing API.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; }

    string? CreatedBy { get; }

    DateTimeOffset? ModifiedAtUtc { get; }

    string? ModifiedBy { get; }

    void SetCreatedAudit(DateTimeOffset occurredAtUtc, string? by);

    void SetModifiedAudit(DateTimeOffset occurredAtUtc, string? by);
}
