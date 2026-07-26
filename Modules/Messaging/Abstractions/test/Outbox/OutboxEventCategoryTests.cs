using RaccoonLand.Modules.Messaging.Abstractions;
using Xunit;

namespace RaccoonLand.Modules.Messaging.Abstractions.Tests.Outbox;

public sealed class OutboxEventCategoryTests
{
    [Fact]
    public void Constants_HaveStableContractValues()
    {
        Assert.Equal("Domain", OutboxEventCategory.Domain);
        Assert.Equal("Service", OutboxEventCategory.Service);
    }

    [Theory]
    [InlineData("Domain", true)]
    [InlineData("Service", true)]
    [InlineData("domain", false)]
    [InlineData("service", false)]
    [InlineData("Unknown", false)]
    [InlineData(null, false)]
    public void IsKnown_ReturnsExpected(string? category, bool expected)
        => Assert.Equal(expected, OutboxEventCategory.IsKnown(category));

    [Theory]
    [InlineData("Domain")]
    [InlineData("Service")]
    public void EnsureKnown_WithKnownValue_ReturnsValue(string category)
        => Assert.Equal(category, OutboxEventCategory.EnsureKnown(category));

    [Theory]
    [InlineData("Unknown")]
    [InlineData("domain")]
    public void EnsureKnown_WithUnknownValue_Throws(string category)
        => Assert.Throws<ArgumentException>(() => OutboxEventCategory.EnsureKnown(category));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureKnown_WithNullOrWhitespace_Throws(string? category)
        => Assert.ThrowsAny<ArgumentException>(() => OutboxEventCategory.EnsureKnown(category));
}
