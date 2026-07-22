using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Dispatch;
using RaccoonLand.Core.RequestProcessing.Tests.Support;

namespace RaccoonLand.Core.RequestProcessing.Tests.Dispatch;

public sealed class EndpointInvokerRegistryTests
{
    [Fact]
    public async Task Resolve_VoidEndpoint_MapsSuccessfulEmptyPayload()
    {
        var registry = new EndpointInvokerRegistry();
        registry.RegisterVoid(typeof(DoSomethingCommand), typeof(DoSomethingEndpoint), RequestKind.Command);

        var services = new ServiceCollection();
        services.AddScoped<DoSomethingEndpoint>();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = new PipelineContext(
            new DoSomethingCommand(),
            RequestKind.Command,
            scope.ServiceProvider);

        await registry.Resolve(typeof(DoSomethingCommand))(context);

        Assert.NotNull(context.Response);
        Assert.Null(context.Response!.Result);
        Assert.Empty(context.Response.Errors);
        Assert.Empty(context.Response.Warnings);
    }

    [Fact]
    public async Task Resolve_ResponseEndpoint_MapsPayloadOntoPipelineResponse()
    {
        var registry = new EndpointInvokerRegistry();
        registry.RegisterResponse(
            typeof(GetSomethingQuery),
            typeof(string),
            typeof(GetSomethingEndpoint),
            RequestKind.Query);

        var services = new ServiceCollection();
        services.AddScoped<GetSomethingEndpoint>();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = new PipelineContext(
            new GetSomethingQuery(),
            RequestKind.Query,
            scope.ServiceProvider);

        await registry.Resolve(typeof(GetSomethingQuery))(context);

        Assert.NotNull(context.Response);
        Assert.Equal("value", context.Response!.Result);
        Assert.Empty(context.Response.Errors);
    }

    [Fact]
    public async Task Resolve_FailureResult_PreservesErrorsWithoutPayload()
    {
        var registry = new EndpointInvokerRegistry();
        registry.RegisterVoid(typeof(FailSomethingCommand), typeof(FailSomethingEndpoint), RequestKind.Command);

        var services = new ServiceCollection();
        services.AddScoped<FailSomethingEndpoint>();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = new PipelineContext(
            new FailSomethingCommand(),
            RequestKind.Command,
            scope.ServiceProvider);

        await registry.Resolve(typeof(FailSomethingCommand))(context);

        Assert.NotNull(context.Response);
        Assert.Null(context.Response!.Result);
        var error = Assert.Single(context.Response.Errors);
        Assert.Equal("CMD_FAIL", error.Code);
        Assert.Equal("command failed", error.Message);
    }

    [Fact]
    public void Resolve_Throws_WhenRequestTypeIsNotRegistered()
    {
        var registry = new EndpointInvokerRegistry();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            registry.Resolve(typeof(DoSomethingCommand)));
        Assert.Contains(typeof(DoSomethingCommand).FullName!, ex.Message);
    }

    [Fact]
    public void ResolveKind_ReturnsExactRegisteredKind_ForVoidAndResponseEndpoints()
    {
        var registry = new EndpointInvokerRegistry();
        registry.RegisterVoid(typeof(DoSomethingCommand), typeof(DoSomethingEndpoint), RequestKind.Command);
        registry.RegisterResponse(
            typeof(GetSomethingQuery),
            typeof(string),
            typeof(GetSomethingEndpoint),
            RequestKind.Query);

        Assert.Equal(RequestKind.Command, registry.ResolveKind(typeof(DoSomethingCommand)));
        Assert.Equal(RequestKind.Query, registry.ResolveKind(typeof(GetSomethingQuery)));
    }

    [Fact]
    public void ResolveKind_Throws_WhenRequestTypeIsNotRegistered()
    {
        var registry = new EndpointInvokerRegistry();

        Assert.Throws<InvalidOperationException>(() =>
            registry.ResolveKind(typeof(DoSomethingCommand)));
    }

    [Fact]
    public void RegisterVoid_Throws_WhenRequestTypeAlreadyRegistered()
    {
        var registry = new EndpointInvokerRegistry();
        registry.RegisterVoid(typeof(DoSomethingCommand), typeof(DoSomethingEndpoint), RequestKind.Command);

        Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterVoid(typeof(DoSomethingCommand), typeof(DoSomethingEndpoint), RequestKind.Command));
    }

    [Fact]
    public void RegisterResponse_Throws_WhenRequestTypeAlreadyRegistered()
    {
        var registry = new EndpointInvokerRegistry();
        registry.RegisterResponse(
            typeof(GetSomethingQuery),
            typeof(string),
            typeof(GetSomethingEndpoint),
            RequestKind.Query);

        Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterResponse(
                typeof(GetSomethingQuery),
                typeof(string),
                typeof(GetSomethingEndpoint),
                RequestKind.Query));
    }

    [Fact]
    public void RegisterResponse_Throws_WhenEndpointIsVoidShape()
    {
        var registry = new EndpointInvokerRegistry();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterResponse(
                typeof(DoSomethingCommand),
                typeof(string),
                typeof(DoSomethingEndpoint),
                RequestKind.Command));

        Assert.Contains(nameof(DoSomethingEndpoint), ex.Message);
        Assert.Contains("IEndpoint", ex.Message);
    }

    [Fact]
    public void RegisterVoid_Throws_WhenEndpointIsResponseShape()
    {
        var registry = new EndpointInvokerRegistry();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterVoid(
                typeof(GetSomethingQuery),
                typeof(GetSomethingEndpoint),
                RequestKind.Query));

        Assert.Contains(nameof(GetSomethingEndpoint), ex.Message);
        Assert.Contains("IEndpoint", ex.Message);
    }
}
