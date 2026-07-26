using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace RaccoonLand.Modules.Messaging.RabbitMQ.Tests.Consuming;

public sealed class ServiceEventMessageMapperTests
{
    private static BasicDeliverEventArgs Delivery(
        IDictionary<string, object?>? headers,
        string body,
        string? messageId = null,
        string? type = null,
        string routingKey = "test.service.event")
    {
        var props = new BasicProperties
        {
            Headers = headers,
            MessageId = messageId,
            Type = type,
        };

        return new BasicDeliverEventArgs(
            consumerTag: "consumer",
            deliveryTag: 1UL,
            redelivered: false,
            exchange: "raccoonland.service-events",
            routingKey: routingKey,
            properties: props,
            body: Encoding.UTF8.GetBytes(body));
    }

    [Fact]
    public void FromDelivery_ReadsHeadersFirst()
    {
        var eventId = Guid.NewGuid();
        var headers = new Dictionary<string, object?>
        {
            [ServiceEventMessageHeaders.EventId] = eventId.ToString(),
            [ServiceEventMessageHeaders.EventType] = "test.service.event",
            [ServiceEventMessageHeaders.EventVersion] = 3,
            [ServiceEventMessageHeaders.AggregateType] = "TestAggregate",
            [ServiceEventMessageHeaders.CreatedBy] = "tester",
        };

        var message = ServiceEventMessageMapper.FromDelivery(Delivery(headers, "{}", routingKey: "svc.key"));

        Assert.Equal(eventId, message.EventId);
        Assert.Equal("test.service.event", message.EventType);
        Assert.Equal(3, message.EventVersion);
        Assert.Equal("TestAggregate", message.AggregateType);
        Assert.Equal("tester", message.CreatedBy);
        Assert.Equal("svc.key", message.RoutingKey);
        Assert.Equal("{}", message.Payload);
    }

    [Fact]
    public void FromDelivery_DecodesByteArrayHeaders()
    {
        var eventId = Guid.NewGuid();
        var headers = new Dictionary<string, object?>
        {
            [ServiceEventMessageHeaders.EventId] = Encoding.UTF8.GetBytes(eventId.ToString()),
            [ServiceEventMessageHeaders.EventType] = Encoding.UTF8.GetBytes("test.service.event"),
        };

        var message = ServiceEventMessageMapper.FromDelivery(Delivery(headers, "{}"));

        Assert.Equal(eventId, message.EventId);
        Assert.Equal("test.service.event", message.EventType);
    }

    [Fact]
    public void FromDelivery_FallsBackToMessageIdAndType()
    {
        var eventId = Guid.NewGuid();

        var message = ServiceEventMessageMapper.FromDelivery(
            Delivery(headers: null, "{}", messageId: eventId.ToString(), type: "test.service.event"));

        Assert.Equal(eventId, message.EventId);
        Assert.Equal("test.service.event", message.EventType);
    }

    [Fact]
    public void FromDelivery_FallsBackToPayload()
    {
        var eventId = Guid.NewGuid();
        var body = $$"""{"eventId":"{{eventId}}","eventType":"test.service.event"}""";

        var message = ServiceEventMessageMapper.FromDelivery(Delivery(headers: null, body));

        Assert.Equal(eventId, message.EventId);
        Assert.Equal("test.service.event", message.EventType);
    }

    [Fact]
    public void FromDelivery_WhenEventIdMissing_Throws()
        => Assert.Throws<InvalidOperationException>(
            () => ServiceEventMessageMapper.FromDelivery(Delivery(headers: null, "{}")));

    [Fact]
    public void FromDelivery_WhenEventTypeMissing_Throws()
    {
        var headers = new Dictionary<string, object?>
        {
            [ServiceEventMessageHeaders.EventId] = Guid.NewGuid().ToString(),
        };

        Assert.Throws<InvalidOperationException>(
            () => ServiceEventMessageMapper.FromDelivery(Delivery(headers, "{}")));
    }
}
