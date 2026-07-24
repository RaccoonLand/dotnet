using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Support;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Pipeline;

public sealed class PipelineContextTests
{
    [Fact]
    public void Constructor_PreservesRequestServicesTokenAndMetadata()
    {
        var request = new SampleRequest();
        var services = new ServiceCollection().BuildServiceProvider();
        var metadata = RequestMetadata.For(typeof(SampleRequest), RequestKind.Command);
        using var cts = new CancellationTokenSource();

        var context = new PipelineContext(request, services, metadata, cts.Token);

        Assert.Same(request, context.Request);
        Assert.Same(services, context.RequestServices);
        Assert.Same(metadata, context.Metadata);
        Assert.Equal(RequestKind.Command, context.Kind);
        Assert.Equal(cts.Token, context.CancellationToken);
    }

    [Fact]
    public void Kind_DelegatesToMetadataKind()
    {
        var metadata = RequestMetadata.For(typeof(SampleQuery), RequestKind.Query);
        var context = new PipelineContext(
            new SampleQuery(),
            new ServiceCollection().BuildServiceProvider(),
            metadata);

        Assert.Equal(RequestKind.Query, context.Kind);
        Assert.Equal(context.Metadata.Kind, context.Kind);
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
        var metadata = RequestMetadata.For(typeof(SampleRequest), RequestKind.Command);

        Assert.Throws<ArgumentNullException>(() =>
            new PipelineContext(null!, services, metadata));
    }

    [Fact]
    public void Constructor_Throws_WhenRequestServicesIsNull()
    {
        var metadata = RequestMetadata.For(typeof(SampleRequest), RequestKind.Command);

        Assert.Throws<ArgumentNullException>(() =>
            new PipelineContext(new SampleRequest(), null!, metadata));
    }

    [Fact]
    public void Constructor_Throws_WhenMetadataIsNull()
    {
        var services = new ServiceCollection().BuildServiceProvider();

        Assert.Throws<ArgumentNullException>(() =>
            new PipelineContext(new SampleRequest(), services, null!));
    }

    [Fact]
    public void Constructor_Throws_WhenMetadataRequestTypeMismatchesRequest()
    {
        // A metadata whose RequestType doesn't match the actual request instance would silently mislead
        // any middleware that reads context.Metadata; fail fast at construction instead.
        var services = new ServiceCollection().BuildServiceProvider();
        var mismatched = RequestMetadata.For(typeof(SampleQuery), RequestKind.Query);

        var exception = Assert.Throws<ArgumentException>(() =>
            new PipelineContext(new SampleRequest(), services, mismatched));

        Assert.Equal("metadata", exception.ParamName);
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
            scope.ServiceProvider,
            RequestMetadata.For(typeof(SampleRequest), RequestKind.Command));

        Assert.Same(scope.ServiceProvider, context.RequestServices);
        Assert.NotSame(root, context.RequestServices);
    }

    private static PipelineContext CreateContext()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new PipelineContext(
            new SampleRequest(),
            services,
            RequestMetadata.For(typeof(SampleRequest), RequestKind.Command));
    }
}
