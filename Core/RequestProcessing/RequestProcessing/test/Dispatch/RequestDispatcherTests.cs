using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Dispatch;
using RaccoonLand.Core.RequestProcessing.Pipeline;
using RaccoonLand.Core.RequestProcessing.Tests.Support;

namespace RaccoonLand.Core.RequestProcessing.Tests.Dispatch;

public sealed class RequestDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_UsesMatchingPipeline_PropagatesTokenAndServices_ForBothKinds()
    {
        using var cts = new CancellationTokenSource();
        var commandTouched = false;
        var queryTouched = false;
        CancellationToken commandToken = default;
        CancellationToken queryToken = default;
        IServiceProvider? commandServices = null;
        IServiceProvider? queryServices = null;
        RequestKind? commandKind = null;
        RequestKind? queryKind = null;

        var (dispatcher, provider) = CreateDispatcher(
            configureCommand: b => b.Use(async (ctx, next) =>
            {
                commandTouched = true;
                commandKind = ctx.Kind;
                commandToken = ctx.CancellationToken;
                commandServices = ctx.RequestServices;
                await next();
            }),
            configureQuery: b => b.Use(async (ctx, next) =>
            {
                queryTouched = true;
                queryKind = ctx.Kind;
                queryToken = ctx.CancellationToken;
                queryServices = ctx.RequestServices;
                await next();
            }));

        using var scope = provider.CreateScope();

        var commandResponse = await dispatcher.DispatchAsync(
            new DoSomethingCommand(),
            scope.ServiceProvider,
            cts.Token);

        Assert.True(commandTouched);
        Assert.False(queryTouched);
        Assert.Equal(RequestKind.Command, commandKind);
        Assert.Equal(cts.Token, commandToken);
        Assert.Same(scope.ServiceProvider, commandServices);
        Assert.NotNull(commandResponse);
        Assert.Empty(commandResponse!.Errors);

        commandTouched = false;
        queryTouched = false;

        var queryResponse = await dispatcher.DispatchAsync(
            new GetSomethingQuery(),
            scope.ServiceProvider,
            cts.Token);

        Assert.False(commandTouched);
        Assert.True(queryTouched);
        Assert.Equal(RequestKind.Query, queryKind);
        Assert.Equal(cts.Token, queryToken);
        Assert.Same(scope.ServiceProvider, queryServices);
        Assert.NotNull(queryResponse);
        Assert.Equal("value", queryResponse!.Result);
    }

    [Fact]
    public async Task DispatchAsync_FailureResponse_PreservesErrorEnvelope()
    {
        var (dispatcher, provider) = CreateDispatcher(
            extraSetup: (services, registry) =>
            {
                services.AddScoped<FailSomethingEndpoint>();
                services.AddScoped<FailGetEndpoint>();
                registry.RegisterVoid(
                    typeof(FailSomethingCommand),
                    typeof(FailSomethingEndpoint),
                    RequestKind.Command);
                registry.RegisterResponse(
                    typeof(FailGetQuery),
                    typeof(string),
                    typeof(FailGetEndpoint),
                    RequestKind.Query);
            });

        using var scope = provider.CreateScope();

        var commandFailure = await dispatcher.DispatchAsync(
            new FailSomethingCommand(),
            scope.ServiceProvider);

        Assert.NotNull(commandFailure);
        Assert.Null(commandFailure!.Result);
        var commandError = Assert.Single(commandFailure.Errors);
        Assert.Equal("CMD_FAIL", commandError.Code);

        var queryFailure = await dispatcher.DispatchAsync(
            new FailGetQuery(),
            scope.ServiceProvider);

        Assert.NotNull(queryFailure);
        Assert.Null(queryFailure!.Result);
        var queryError = Assert.Single(queryFailure.Errors);
        Assert.Equal("QRY_FAIL", queryError.Code);
    }

    [Fact]
    public async Task DispatchAsync_ReturnsNull_WhenPipelineLeavesResponseUnset()
    {
        var registry = new EndpointInvokerRegistry();
        registry.RegisterVoid(typeof(DoSomethingCommand), typeof(DoSomethingEndpoint), RequestKind.Command);

        var services = new ServiceCollection();
        services.AddScoped<DoSomethingEndpoint>();
        var provider = services.BuildServiceProvider();

        // Command pipeline short-circuits without setting Response; query unused but required.
        PipelineDelegate command = async ctx =>
        {
            await Task.CompletedTask;
            // intentionally leave ctx.Response null
        };
        PipelineDelegate query = _ => Task.CompletedTask;

        var dispatcher = new RequestDispatcher(new CompiledPipelines(command, query), registry);

        using var scope = provider.CreateScope();
        var response = await dispatcher.DispatchAsync(new DoSomethingCommand(), scope.ServiceProvider);

        Assert.Null(response);
    }

    [Fact]
    public void Constructor_Throws_WhenPipelinesOrRegistryNull()
    {
        var pipelines = new CompiledPipelines(_ => Task.CompletedTask, _ => Task.CompletedTask);
        var registry = new EndpointInvokerRegistry();

        Assert.Throws<ArgumentNullException>(() => new RequestDispatcher(null!, registry));
        Assert.Throws<ArgumentNullException>(() => new RequestDispatcher(pipelines, null!));
    }

    private static (RequestDispatcher Dispatcher, ServiceProvider Provider) CreateDispatcher(
        Action<IPipelineBuilder>? configureCommand = null,
        Action<IPipelineBuilder>? configureQuery = null,
        Action<IServiceCollection, EndpointInvokerRegistry>? extraSetup = null)
    {
        var registry = new EndpointInvokerRegistry();
        registry.RegisterVoid(typeof(DoSomethingCommand), typeof(DoSomethingEndpoint), RequestKind.Command);
        registry.RegisterResponse(
            typeof(GetSomethingQuery),
            typeof(string),
            typeof(GetSomethingEndpoint),
            RequestKind.Query);

        var services = new ServiceCollection();
        services.AddScoped<DoSomethingEndpoint>();
        services.AddScoped<GetSomethingEndpoint>();
        extraSetup?.Invoke(services, registry);

        var provider = services.BuildServiceProvider();

        var commandBuilder = new CommandPipelineBuilder(provider, registry);
        configureCommand?.Invoke(commandBuilder);
        var queryBuilder = new QueryPipelineBuilder(provider, registry);
        configureQuery?.Invoke(queryBuilder);

        var dispatcher = new RequestDispatcher(
            new CompiledPipelines(commandBuilder.Build(), queryBuilder.Build()),
            registry);

        return (dispatcher, provider);
    }
}
