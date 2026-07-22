using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Dispatch;
using RaccoonLand.Core.RequestProcessing.Pipeline;
using RaccoonLand.Core.RequestProcessing.Tests.Support;

namespace RaccoonLand.Core.RequestProcessing.Tests.Pipeline;

public sealed class CommandQueryPipelineIndependenceTests
{
    [Fact]
    public async Task Dispatcher_RunsOnlyMiddlewareFromSelectedPipeline()
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
        var provider = services.BuildServiceProvider();

        var commandHits = 0;
        var queryHits = 0;

        var commandBuilder = new CommandPipelineBuilder(provider, registry);
        commandBuilder.Use(async (ctx, next) =>
        {
            commandHits++;
            ctx.Items["side"] = "command";
            await next();
        });

        var queryBuilder = new QueryPipelineBuilder(provider, registry);
        queryBuilder.Use(async (ctx, next) =>
        {
            queryHits++;
            ctx.Items["side"] = "query";
            await next();
        });

        var dispatcher = new RequestDispatcher(
            new CompiledPipelines(commandBuilder.Build(), queryBuilder.Build()),
            registry);

        using var scope = provider.CreateScope();

        await dispatcher.DispatchAsync(new DoSomethingCommand(), scope.ServiceProvider);
        Assert.Equal(1, commandHits);
        Assert.Equal(0, queryHits);

        await dispatcher.DispatchAsync(new GetSomethingQuery(), scope.ServiceProvider);
        Assert.Equal(1, commandHits);
        Assert.Equal(1, queryHits);
    }

    [Fact]
    public async Task CommandAndQueryBuilders_ResolveInvokerFromSharedRegistry()
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
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var commandPipeline = new CommandPipelineBuilder(provider, registry).Build();
        var queryPipeline = new QueryPipelineBuilder(provider, registry).Build();

        var commandContext = new PipelineContext(
            new DoSomethingCommand(),
            RequestKind.Command,
            scope.ServiceProvider);
        await commandPipeline(commandContext);
        Assert.NotNull(commandContext.Response);

        var queryContext = new PipelineContext(
            new GetSomethingQuery(),
            RequestKind.Query,
            scope.ServiceProvider);
        await queryPipeline(queryContext);
        Assert.Equal("value", queryContext.Response!.Result);
    }
}
