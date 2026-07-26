using RaccoonLand.Modules.Messaging.Abstractions;
using Xunit;

namespace RaccoonLand.Modules.Messaging.Abstractions.Tests.Outbox;

public sealed class OutboxEventRecordTests
{
    private static OutboxEventRecord CreateValid(Guid eventId, DateTimeOffset claimedOn) => new()
    {
        EventId = eventId,
        Category = OutboxEventCategory.Domain,
        EventType = "test.event",
        AggregateType = "TestAggregate",
        Payload = "{}",
        ClaimedOnUtc = claimedOn,
    };

    [Fact]
    public void ToClaim_CarriesEventIdAndClaimStamp()
    {
        var eventId = Guid.NewGuid();
        var claimedOn = DateTimeOffset.UtcNow;
        var record = CreateValid(eventId, claimedOn);

        var claim = record.ToClaim();

        Assert.Equal(eventId, claim.EventId);
        Assert.Equal(claimedOn, claim.ClaimedOnUtc);
    }

    [Fact]
    public void Category_WithUnknownValue_Throws()
        => Assert.Throws<ArgumentException>(() => new OutboxEventRecord
        {
            Category = "Bogus",
            EventType = "test.event",
            AggregateType = "TestAggregate",
            Payload = "{}",
        });

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EventType_WithBlankValue_Throws(string eventType)
        => Assert.Throws<ArgumentException>(() => new OutboxEventRecord
        {
            Category = OutboxEventCategory.Service,
            EventType = eventType,
            AggregateType = "TestAggregate",
            Payload = "{}",
        });

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AggregateType_WithBlankValue_Throws(string aggregateType)
        => Assert.Throws<ArgumentException>(() => new OutboxEventRecord
        {
            Category = OutboxEventCategory.Service,
            EventType = "test.event",
            AggregateType = aggregateType,
            Payload = "{}",
        });

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Payload_WithBlankValue_Throws(string payload)
        => Assert.Throws<ArgumentException>(() => new OutboxEventRecord
        {
            Category = OutboxEventCategory.Service,
            EventType = "test.event",
            AggregateType = "TestAggregate",
            Payload = payload,
        });
}
