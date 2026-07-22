using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Tests.Support;

namespace RaccoonLand.Core.Domain.Tests.Abstractions;

public sealed class EntityAuditTests
{
    [Fact]
    public void SetCreatedAudit_SetsCreatedAtUtcAndCreatedBy()
    {
        var entity = new TestEntity(1);
        IAuditable auditable = entity;
        var occurredAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

        auditable.SetCreatedAudit(occurredAt, "creator");

        Assert.Equal(occurredAt, entity.CreatedAtUtc);
        Assert.Equal("creator", entity.CreatedBy);
    }

    [Fact]
    public void SetModifiedAudit_SetsModifiedAtUtcAndModifiedBy()
    {
        var entity = new TestEntity(1);
        IAuditable auditable = entity;
        var occurredAt = new DateTimeOffset(2026, 7, 22, 13, 0, 0, TimeSpan.Zero);

        auditable.SetModifiedAudit(occurredAt, "modifier");

        Assert.Equal(occurredAt, entity.ModifiedAtUtc);
        Assert.Equal("modifier", entity.ModifiedBy);
    }

    [Fact]
    public void SetCreatedAudit_AllowsNullBy()
    {
        var entity = new TestEntity(1);
        IAuditable auditable = entity;
        var occurredAt = DateTimeOffset.UtcNow;

        auditable.SetCreatedAudit(occurredAt, null);

        Assert.Equal(occurredAt, entity.CreatedAtUtc);
        Assert.Null(entity.CreatedBy);
    }

    [Fact]
    public void SetModifiedAudit_AllowsNullBy()
    {
        var entity = new TestEntity(1);
        IAuditable auditable = entity;
        var occurredAt = DateTimeOffset.UtcNow;

        auditable.SetModifiedAudit(occurredAt, null);

        Assert.Equal(occurredAt, entity.ModifiedAtUtc);
        Assert.Null(entity.ModifiedBy);
    }
}
