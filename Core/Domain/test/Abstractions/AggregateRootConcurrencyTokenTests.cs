using RaccoonLand.Core.Domain.Tests.Support;

namespace RaccoonLand.Core.Domain.Tests.Abstractions;

public sealed class AggregateRootConcurrencyTokenTests
{
    [Fact]
    public void ConcurrencyToken_IsNonDefault_WhenAggregateIsCreated()
    {
        var aggregate = new TestAggregateRoot();

        Assert.NotEqual(Guid.Empty, aggregate.ConcurrencyToken);
    }

    [Fact]
    public void RegenerateConcurrencyToken_ReplacesPreviousValueWithNewOne()
    {
        var aggregate = new TestAggregateRoot(1);
        var original = aggregate.ConcurrencyToken;

        aggregate.RegenerateConcurrencyToken();
        var firstRegen = aggregate.ConcurrencyToken;

        aggregate.RegenerateConcurrencyToken();
        var secondRegen = aggregate.ConcurrencyToken;

        Assert.NotEqual(original, firstRegen);
        Assert.NotEqual(firstRegen, secondRegen);
        Assert.NotEqual(Guid.Empty, firstRegen);
        Assert.NotEqual(Guid.Empty, secondRegen);
    }
}
