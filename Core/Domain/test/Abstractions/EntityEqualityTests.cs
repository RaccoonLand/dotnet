using RaccoonLand.Core.Domain.Tests.Support;

namespace RaccoonLand.Core.Domain.Tests.Abstractions;

public sealed class EntityEqualityTests
{
    [Fact]
    public void Equals_ReturnsFalse_WhenOtherIsNull()
    {
        var entity = new TestEntity(1);

        Assert.False(entity.Equals(null));
        Assert.False(entity.Equals((object?)null));
    }

    [Fact]
    public void Equals_ReturnsTrue_WhenSameInstance()
    {
        var entity = new TestEntity(1);

        Assert.True(entity.Equals(entity));
        Assert.True(entity.Equals((object)entity));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenRuntimeTypesDiffer_EvenWithSameId()
    {
        var left = new TestEntity(10);
        var right = new OtherTestEntity(10);

        Assert.False(left.Equals(right));
        Assert.False(right.Equals(left));
    }

    [Fact]
    public void Equals_ReturnsTrueOnlyByReference_WhenEntitiesAreTransient()
    {
        var first = new TestEntity();
        var second = new TestEntity();

        Assert.True(first.Equals(first));
        Assert.False(first.Equals(second));
        Assert.False(second.Equals(first));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenOneIsTransientAndOtherIsPersisted()
    {
        var transient = new TestEntity();
        var persisted = new TestEntity(1);

        Assert.False(transient.Equals(persisted));
        Assert.False(persisted.Equals(transient));
    }

    [Fact]
    public void Equals_ReturnsTrue_WhenSameRuntimeTypeAndSamePersistedId()
    {
        var left = new TestEntity(42);
        var right = new TestEntity(42);

        Assert.True(left.Equals(right));
        Assert.True(right.Equals(left));
    }

    [Fact]
    public void EqualityOperators_MatchEquals()
    {
        var left = new TestEntity(5);
        var right = new TestEntity(5);
        var different = new TestEntity(6);
        TestEntity? nullEntity = null;

        Assert.True(left == right);
        Assert.False(left != right);
        Assert.False(left == different);
        Assert.True(left != different);
        Assert.False(left == nullEntity);
        Assert.True(left != nullEntity);
        Assert.True(nullEntity == null);
        Assert.False(nullEntity != null);
    }

    [Fact]
    public void GetHashCode_Matches_WhenPersistedEntitiesAreEqual()
    {
        var left = new TestEntity(7);
        var right = new TestEntity(7);

        Assert.True(left.Equals(right));
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void HashSet_TreatsEqualPersistedEntitiesAsOneEntry()
    {
        var left = new TestEntity(7);
        var right = new TestEntity(7);

        var set = new HashSet<TestEntity> { left, right };

        Assert.Single(set);
        Assert.Contains(left, set);
        Assert.Contains(right, set);
    }

    [Fact]
    public void HashSet_TreatsDistinctTransientEntitiesAsSeparateEntries()
    {
        var first = new TestEntity();
        var second = new TestEntity();

        var set = new HashSet<TestEntity> { first, second };

        Assert.Equal(2, set.Count);
        Assert.Contains(first, set);
        Assert.Contains(second, set);
    }

    [Fact]
    public void HashSet_DoesNotEquateTransientEntityWithPersistedEntity()
    {
        var transient = new TestEntity();
        var persisted = new TestEntity(1);

        var set = new HashSet<TestEntity> { transient, persisted };

        Assert.Equal(2, set.Count);
        Assert.False(transient.Equals(persisted));
        Assert.Contains(transient, set);
        Assert.Contains(persisted, set);
    }
}
