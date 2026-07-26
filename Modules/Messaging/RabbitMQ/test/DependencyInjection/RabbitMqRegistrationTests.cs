using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace RaccoonLand.Modules.Messaging.RabbitMQ.Tests.DependencyInjection;

public sealed class RabbitMqRegistrationTests
{
    private static RabbitMqServiceEventConsumerOptions ResolveConsumer(
        Action<RabbitMqServiceEventConsumerOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandRabbitMqServiceEventConsumer(configure);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<RabbitMqServiceEventConsumerOptions>>().Value;
    }

    private static RabbitMqServiceEventOptions ResolvePublisher(Action<RabbitMqServiceEventOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandRabbitMqServiceEvents(configure);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<RabbitMqServiceEventOptions>>().Value;
    }

    [Fact]
    public void Consumer_WithValidConfig_ValidatesSuccessfully()
    {
        var options = ResolveConsumer(o => o.QueueName = "orders");

        Assert.Equal("orders", options.QueueName);
    }

    [Fact]
    public void Consumer_WhenQueueNameMissing_FailsValidationWithNamedField()
    {
        var ex = Assert.Throws<OptionsValidationException>(() => ResolveConsumer(_ => { }));

        Assert.Contains(
            ex.Failures,
            f => f.Contains(nameof(RabbitMqServiceEventConsumerOptions.QueueName), StringComparison.Ordinal));
    }

    [Fact]
    public void Consumer_WhenMaxDeliveryAttemptsPositiveWithoutDeadLetter_FailsValidation()
    {
        var ex = Assert.Throws<OptionsValidationException>(() => ResolveConsumer(o =>
        {
            o.QueueName = "orders";
            o.MaxDeliveryAttempts = 3;
            o.EnableDeadLetterTopology = false;
            o.DeadLetterExchangeName = string.Empty;
        }));

        Assert.Contains(
            ex.Failures,
            f => f.Contains(nameof(RabbitMqServiceEventConsumerOptions.MaxDeliveryAttempts), StringComparison.Ordinal));
    }

    [Fact]
    public void Consumer_WhenMaxDeliveryAttemptsZeroWithRequeue_FailsValidationForHotLoop()
    {
        var ex = Assert.Throws<OptionsValidationException>(() => ResolveConsumer(o =>
        {
            o.QueueName = "orders";
            o.MaxDeliveryAttempts = 0;
            o.RequeueOnFailure = true;
        }));

        Assert.Contains(
            ex.Failures,
            f => f.Contains(nameof(RabbitMqServiceEventConsumerOptions.RequeueOnFailure), StringComparison.Ordinal));
    }

    [Fact]
    public void Consumer_WhenMaxDeliveryAttemptsZeroWithoutRequeue_ValidatesSuccessfully()
    {
        var options = ResolveConsumer(o =>
        {
            o.QueueName = "orders";
            o.MaxDeliveryAttempts = 0;
            o.RequeueOnFailure = false;
        });

        Assert.Equal(0, options.MaxDeliveryAttempts);
        Assert.False(options.RequeueOnFailure);
    }

    [Fact]
    public void Publisher_WithDefaults_ValidatesSuccessfully()
    {
        var options = ResolvePublisher(_ => { });

        Assert.Equal("raccoonland.service-events", options.ExchangeName);
    }

    [Fact]
    public void Publisher_WhenExchangeNameMissing_FailsValidationWithNamedField()
    {
        var ex = Assert.Throws<OptionsValidationException>(() => ResolvePublisher(o => o.ExchangeName = string.Empty));

        Assert.Contains(
            ex.Failures,
            f => f.Contains(nameof(RabbitMqServiceEventOptions.ExchangeName), StringComparison.Ordinal));
    }

    [Fact]
    public void Publisher_WhenUriInvalid_FailsValidation()
    {
        var ex = Assert.Throws<OptionsValidationException>(() => ResolvePublisher(o => o.Uri = "not-a-valid-uri"));

        Assert.NotEmpty(ex.Failures);
    }
}
