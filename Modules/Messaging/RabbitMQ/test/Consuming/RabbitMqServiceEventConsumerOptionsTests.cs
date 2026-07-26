using Xunit;

namespace RaccoonLand.Modules.Messaging.RabbitMQ.Tests.Consuming;

public sealed class RabbitMqServiceEventConsumerOptionsTests
{
    [Fact]
    public void ResolveDeadLetterExchangeName_WhenUnset_DerivesFromQueue()
    {
        var options = new RabbitMqServiceEventConsumerOptions { QueueName = "orders" };

        Assert.Equal("orders.dlx", options.ResolveDeadLetterExchangeName());
    }

    [Fact]
    public void ResolveDeadLetterExchangeName_WhenSet_UsesExplicitValue()
    {
        var options = new RabbitMqServiceEventConsumerOptions
        {
            QueueName = "orders",
            DeadLetterExchangeName = "custom.dlx",
        };

        Assert.Equal("custom.dlx", options.ResolveDeadLetterExchangeName());
    }

    [Fact]
    public void ResolveDeadLetterQueueName_WhenUnset_DerivesFromQueue()
    {
        var options = new RabbitMqServiceEventConsumerOptions { QueueName = "orders" };

        Assert.Equal("orders.poison", options.ResolveDeadLetterQueueName());
    }

    [Fact]
    public void ResolveDeadLetterQueueName_WhenSet_UsesExplicitValue()
    {
        var options = new RabbitMqServiceEventConsumerOptions
        {
            QueueName = "orders",
            DeadLetterQueueName = "custom.poison",
        };

        Assert.Equal("custom.poison", options.ResolveDeadLetterQueueName());
    }
}
