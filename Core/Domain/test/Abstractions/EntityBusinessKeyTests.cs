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
    public void BusinessKey_DoesNotChange_WhenIdIsAssignedOrChanged()
    {
        var entity = new TestEntity();
        var originalBusinessKey = entity.BusinessKey;

        entity.SetId(1);
        Assert.Equal(originalBusinessKey, entity.BusinessKey);

        entity.SetId(2);
        Assert.Equal(originalBusinessKey, entity.BusinessKey);
    }
}
