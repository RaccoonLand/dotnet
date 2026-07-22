using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Dispatch;
using RaccoonLand.Core.RequestProcessing.Tests.Support;

namespace RaccoonLand.Core.RequestProcessing.Tests.Dispatch;

public sealed class RequestServicesResolutionTests
{
    [Fact]
    public async Task EndpointInvoker_ResolvesEndpointFromRequestServices_NotRoot()
    {
        var registry = new EndpointInvokerRegistry();
        registry.RegisterVoid(typeof(MarkerAwareCommand), typeof(MarkerAwareEndpoint), RequestKind.Command);

        var root = new ServiceCollection().BuildServiceProvider();

        var requestServices = new ServiceCollection();
        requestServices.AddScoped<ScopedMarker>();
        requestServices.AddScoped<MarkerAwareEndpoint>();
        var requestProvider = requestServices.BuildServiceProvider();
        using var scope = requestProvider.CreateScope();

        var expectedMarker = scope.ServiceProvider.GetRequiredService<ScopedMarker>();
        var context = new PipelineContext(
            new MarkerAwareCommand(),
            RequestKind.Command,
            scope.ServiceProvider);

        await registry.Resolve(typeof(MarkerAwareCommand))(context);

        var endpoint = scope.ServiceProvider.GetRequiredService<MarkerAwareEndpoint>();
        Assert.Same(expectedMarker, endpoint.Marker);
        Assert.NotNull(context.Response);

        var rootContext = new PipelineContext(
            new MarkerAwareCommand(),
            RequestKind.Command,
            root);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => registry.Resolve(typeof(MarkerAwareCommand))(rootContext));
    }
}
