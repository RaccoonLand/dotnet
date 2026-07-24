using RaccoonLand.Core.Domain.Tests.Support;

namespace RaccoonLand.Core.Domain.Tests.Abstractions;

public sealed class EntityBusinessKeyTests
{
    [Fact]
    public void BusinessKey_IsNonDefault_WhenEntityIsCreated()
    {
        var entity = new TestEntity();

        Assert.NotEqual(Guid.Empty, entity.BusinessKey);
    }

    [Fact]
    public void BusinessKey_Differs_BetweenNewEntities()
    {
        var first = new TestEntity();
        var second = new TestEntity();

        Assert.NotEqual(first.BusinessKey, second.BusinessKey);
    }

    [Fact]
    public void BusinessKey_IsStable_AcrossRepeatedReadsOnSameInstance()
    {
        // BusinessKey is init-only; once set at construction it must not change for the lifetime
        // of the instance (events reference it and outbox rows use it as the source identifier).
        var entity = new TestEntity();

        var first = entity.BusinessKey;
        var second = entity.BusinessKey;
        var third = entity.BusinessKey;

        Assert.Equal(first, second);
        Assert.Equal(second, third);
    }

    [Fact]
    public void BusinessKey_IsIndependentOfId_ForBothTransientAndPersistedEntities()
    {
        // BusinessKey is generated up-front and does not depend on Id being assigned; two entities
        // with the same Id still have distinct business keys, and a transient entity already has one.
        var transient = new TestEntity();
        var persistedA = new TestEntity(1);
        var persistedB = new TestEntity(1);

        Assert.NotEqual(Guid.Empty, transient.BusinessKey);
        Assert.NotEqual(persistedA.BusinessKey, persistedB.BusinessKey);
        Assert.NotEqual(transient.BusinessKey, persistedA.BusinessKey);
    }
}
