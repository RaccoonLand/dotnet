using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Messaging.Abstractions;
using Xunit;

namespace RaccoonLand.Modules.Messaging.OutboxRelay.Tests.DependencyInjection;

public sealed class OutboxRelayRegistrationTests
{
    [Fact]
    public void AddRaccoonLandOutboxRelay_WithDefaults_ValidatesSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandOutboxRelay();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OutboxRelayOptions>>();

        Assert.Equal(20, options.Value.BatchSize);
    }

    [Fact]
    public void AddRaccoonLandOutboxRelay_WhenBatchSizeNonPositive_FailsValidationWithNamedField()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandOutboxRelay(o => o.BatchSize = 0);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OutboxRelayOptions>>();

        var ex = Assert.Throws<OptionsValidationException>(() => _ = options.Value);
        Assert.Contains(
            ex.Failures,
            f => f.Contains(nameof(OutboxRelayOptions.BatchSize), StringComparison.Ordinal));
    }

    [Fact]
    public void AddRaccoonLandOutboxRelay_WhenClaimLeaseBelowOneSecond_FailsValidationWithNamedField()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandOutboxRelay(o => o.ClaimLease = TimeSpan.FromMilliseconds(500));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OutboxRelayOptions>>();

        var ex = Assert.Throws<OptionsValidationException>(() => _ = options.Value);
        Assert.Contains(
            ex.Failures,
            f => f.Contains(nameof(OutboxRelayOptions.ClaimLease), StringComparison.Ordinal));
    }

    [Fact]
    public void AddRaccoonLandOutboxRelay_WhenPollIntervalNegative_FailsValidationWithNamedField()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandOutboxRelay(o => o.PollInterval = TimeSpan.FromSeconds(-1));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OutboxRelayOptions>>();

        var ex = Assert.Throws<OptionsValidationException>(() => _ = options.Value);
        Assert.Contains(
            ex.Failures,
            f => f.Contains(nameof(OutboxRelayOptions.PollInterval), StringComparison.Ordinal));
    }
}
