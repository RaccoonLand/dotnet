using Microsoft.Extensions.Options;
using RaccoonLand.Core.Hosting.AspNetCore.HttpExceptionHandling;

namespace RaccoonLand.Core.Hosting.AspNetCore.Tests.HttpExceptionHandling;

public sealed class HttpExceptionHandlingServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRaccoonLandHttpExceptionHandling_Throws_WhenServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HttpExceptionHandlingServiceCollectionExtensions.AddRaccoonLandHttpExceptionHandling(null!));
    }

    [Fact]
    public void AddRaccoonLandHttpExceptionHandling_WithoutConfigure_RegistersEmptyOptions()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandHttpExceptionHandling();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HttpExceptionHandlingOptions>>().Value;

        Assert.NotNull(options);
        Assert.Empty(options.Handlers);
    }

    [Fact]
    public void AddRaccoonLandHttpExceptionHandling_WithConfigure_AppliesConfiguration()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandHttpExceptionHandling(o =>
            o.On<InvalidOperationException>((_, _) => Task.FromResult(true)));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HttpExceptionHandlingOptions>>().Value;

        var registration = Assert.Single(options.Handlers);
        Assert.Equal(typeof(InvalidOperationException), registration.ExceptionType);
    }

    [Fact]
    public void AddRaccoonLandHttpExceptionHandling_ComposesConfigureCallbacks_AcrossCalls()
    {
        // Options composition contract: calling the extension more than once must accumulate
        // handler registrations, not replace them. This is what allows a host plugin to add its
        // own On<T> handler after the application has already called AddRaccoonLandHttpExceptionHandling.
        var services = new ServiceCollection();

        services.AddRaccoonLandHttpExceptionHandling(o =>
            o.On<InvalidOperationException>((_, _) => Task.FromResult(true)));
        services.AddRaccoonLandHttpExceptionHandling(o =>
            o.On<ArgumentException>((_, _) => Task.FromResult(true)));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HttpExceptionHandlingOptions>>().Value;

        Assert.Equal(
            new[] { typeof(InvalidOperationException), typeof(ArgumentException) },
            options.Handlers.Select(h => h.ExceptionType).ToArray());
    }
}
