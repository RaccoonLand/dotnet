using RaccoonLand.Core.Domain.Tests.Support;

namespace RaccoonLand.Core.Domain.Tests.Abstractions;

public sealed class ValueObjectTests
{
    [Fact]
    public void Equals_ReturnsTrue_WhenComponentsAreEqualAndInSameOrder()
    {
        var left = new TestMoney("USD", 10m);
        var right = new TestMoney("USD", 10m);

        Assert.True(left.Equals(right));
        Assert.True(right.Equals(left));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenComponentValuesDiffer()
    {
        var left = new TestMoney("USD", 10m);
        var right = new TestMoney("USD", 11m);

        Assert.False(left.Equals(right));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenComponentOrderDiffers()
    {
        var left = new OrderedValueObject("A", "B");
        var right = new OrderedValueObject("B", "A");

        Assert.False(left.Equals(right));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenRuntimeTypesDiffer()
    {
        var left = new TestMoney("USD", 10m);
        var right = new OtherTestMoney("USD", 10m);

        Assert.False(left.Equals(right));
        Assert.False(right.Equals(left));
    }

    [Fact]
    public void Equals_HandlesNullAndSelfReference()
    {
        var value = new TestMoney("EUR", 5m);

        Assert.True(value.Equals(value));
        Assert.False(value.Equals(null));
        Assert.False(value.Equals((object?)null));
    }

    [Fact]
    public void Equals_TreatsNullComponentsAsEqualWhenCorresponding()
    {
        var left = new OrderedValueObject(null, "B");
        var right = new OrderedValueObject(null, "B");
        var different = new OrderedValueObject("A", "B");

        Assert.True(left.Equals(right));
        Assert.False(left.Equals(different));
    }

    [Fact]
    public void EqualityOperators_MatchEquals()
    {
        var left = new TestMoney("USD", 10m);
        var right = new TestMoney("USD", 10m);
        var different = new TestMoney("USD", 20m);
        TestMoney? nullValue = null;

        Assert.True(left == right);
        Assert.False(left != right);
        Assert.False(left == different);
        Assert.True(left != different);
        Assert.False(left == nullValue);
        Assert.True(left != nullValue);
        Assert.True(nullValue == null);
        Assert.False(nullValue != null);
    }

    [Fact]
    public void GetHashCode_Matches_WhenValueObjectsAreEqual()
    {
        var left = new TestMoney("USD", 10m);
        var right = new TestMoney("USD", 10m);

        Assert.True(left.Equals(right));
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void HashSet_TreatsEqualValueObjectsAsOneEntry()
    {
        var left = new TestMoney("USD", 10m);
        var right = new TestMoney("USD", 10m);
        var different = new TestMoney("USD", 20m);

        var set = new HashSet<TestMoney> { left, right, different };

        Assert.Equal(2, set.Count);
        Assert.Contains(left, set);
        Assert.Contains(different, set);
    }

    private sealed class OrderedValueObject : RaccoonLand.Core.Domain.Abstractions.ValueObject
    {
        private readonly string? _first;
        private readonly string? _second;

        public OrderedValueObject(string? first, string? second)
        {
            _first = first;
            _second = second;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return _first;
            yield return _second;
        }
    }
}
