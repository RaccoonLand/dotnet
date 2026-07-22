using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Support;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Pipeline;

public sealed class PipelineContextTests
{
    [Fact]
    public void Constructor_PreservesRequestKindServicesAndToken()
    {
        var request = new SampleRequest();
        var services = new ServiceCollection().BuildServiceProvider();
        using var cts = new CancellationTokenSource();

        var context = new PipelineContext(request, RequestKind.Command, services, cts.Token);

        Assert.Same(request, context.Request);
        Assert.Equal(RequestKind.Command, context.Kind);
        Assert.Same(services, context.RequestServices);
        Assert.Equal(cts.Token, context.CancellationToken);
    }

    [Fact]
    public void Response_IsNull_UntilSet()
    {
        var context = CreateContext();

        Assert.Null(context.Response);
    }

    [Fact]
    public void Constructor_Throws_WhenRequestIsNull()
    {
        var services = new ServiceCollection().BuildServiceProvider();

        Assert.Throws<ArgumentNullException>(() =>
            new PipelineContext(null!, RequestKind.Command, services));
    }

    [Fact]
    public void Constructor_Throws_WhenRequestServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PipelineContext(new SampleRequest(), RequestKind.Command, null!));
    }

    [Fact]
    public void Contexts_AreIndependent()
    {
        var first = CreateContext();
        var second = CreateContext();

        first.Items["key"] = "a";
        first.Response = new PipelineResponse { Result = 1 };

        Assert.False(second.Items.ContainsKey("key"));
        Assert.Null(second.Response);
        Assert.NotSame(first.Items, second.Items);
        Assert.NotSame(first.RequestServices, second.RequestServices);
    }

    [Fact]
    public void RequestServices_IsTheSameInstancePassedToConstructor()
    {
        var root = new ServiceCollection().BuildServiceProvider();
        using var scope = root.CreateScope();

        var context = new PipelineContext(
            new SampleRequest(),
            RequestKind.Command,
            scope.ServiceProvider);

        Assert.Same(scope.ServiceProvider, context.RequestServices);
        Assert.NotSame(root, context.RequestServices);
    }

    private static PipelineContext CreateContext()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new PipelineContext(new SampleRequest(), RequestKind.Command, services);
    }
}
