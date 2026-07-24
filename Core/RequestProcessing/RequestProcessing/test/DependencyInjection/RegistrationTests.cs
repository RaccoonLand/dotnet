using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Dispatch;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.DependencyInjection;
using RaccoonLand.Core.RequestProcessing.Dispatch;
using RaccoonLand.Core.RequestProcessing.Pipeline;
using RaccoonLand.Core.RequestProcessing.Tests.Support;

namespace RaccoonLand.Core.RequestProcessing.Tests.DependencyInjection;

public sealed class RegistrationTests
{
    [Fact]
    public void AddRaccoonLandRequestProcessing_ScansAssembly_AndRegistersEndpoints()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandRequestProcessing(scanAssemblies: typeof(DoSomethingEndpoint).Assembly);

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<EndpointInvokerRegistry>();

        Assert.Equal(RequestKind.Command, registry.ResolveKind(typeof(DoSomethingCommand)));
        Assert.Equal(RequestKind.Query, registry.ResolveKind(typeof(GetSomethingQuery)));
        Assert.NotNull(provider.GetService<IRequestDispatcher>());
    }

    [Fact]
    public void AddRaccoonLandRequestProcessing_DeduplicatesAssemblies()
    {
        var assembly = typeof(DoSomethingEndpoint).Assembly;
        var services = new ServiceCollection();

        services.AddRaccoonLandRequestProcessing(scanAssemblies: [assembly, assembly]);

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<EndpointInvokerRegistry>();
        Assert.Equal(RequestKind.Command, registry.ResolveKind(typeof(DoSomethingCommand)));
    }

    [Fact]
    public void AddRaccoonLandRequestProcessing_Throws_WhenAssemblyIsNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddRaccoonLandRequestProcessing(scanAssemblies: [null!]));
    }

    [Fact]
    public void AddRaccoonLandRequestProcessing_AppliesPipelineCallbacks_AndBuildsLazily()
    {
        var commandConfigured = false;
        var queryConfigured = false;

        var services = new ServiceCollection();
        services.AddRaccoonLandRequestProcessing(
            configureCommandPipeline: b =>
            {
                commandConfigured = true;
                b.Use(async (ctx, next) => await next());
            },
            configureQueryPipeline: b =>
            {
                queryConfigured = true;
                b.Use(async (ctx, next) => await next());
            },
            scanAssemblies: typeof(DoSomethingEndpoint).Assembly);

        Assert.False(commandConfigured);
        Assert.False(queryConfigured);

        var provider = services.BuildServiceProvider();
        Assert.False(commandConfigured);
        Assert.False(queryConfigured);

        var pipelines = provider.GetRequiredService<CompiledPipelines>();
        Assert.True(commandConfigured);
        Assert.True(queryConfigured);
        Assert.NotNull(pipelines.Command);
        Assert.NotNull(pipelines.Query);

        var again = provider.GetRequiredService<CompiledPipelines>();
        Assert.Same(pipelines, again);
    }

    [Fact]
    public async Task AddRaccoonLandRequestProcessing_RegisteredMiddleware_RunsOnDispatch_WithOrder()
    {
        var commandSequence = new List<string>();
        var querySequence = new List<string>();

        var services = new ServiceCollection();
        services.AddRaccoonLandRequestProcessing(
            configureCommandPipeline: b =>
            {
                b.Use(async (ctx, next) =>
                {
                    commandSequence.Add("cmd-before");
                    await next();
                    commandSequence.Add("cmd-after");
                });
            },
            configureQueryPipeline: b =>
            {
                b.Use(async (ctx, next) =>
                {
                    querySequence.Add("qry-before");
                    await next();
                    querySequence.Add("qry-after");
                });
            },
            scanAssemblies: typeof(DoSomethingEndpoint).Assembly);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IRequestDispatcher>();

        await dispatcher.DispatchAsync(new DoSomethingCommand(), scope.ServiceProvider);
        Assert.Equal(["cmd-before", "cmd-after"], commandSequence);
        Assert.Empty(querySequence);

        await dispatcher.DispatchAsync(new GetSomethingQuery(), scope.ServiceProvider);
        Assert.Equal(["cmd-before", "cmd-after"], commandSequence);
        Assert.Equal(["qry-before", "qry-after"], querySequence);
    }

    [Fact]
    public async Task AddRaccoonLandRequestProcessing_CompiledPipelines_DispatchWorks()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandRequestProcessing(scanAssemblies: typeof(DoSomethingEndpoint).Assembly);
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IRequestDispatcher>();
        var response = await dispatcher.DispatchAsync(new GetSomethingQuery(), scope.ServiceProvider);

        Assert.Equal("value", response!.Result);
    }

    [Fact]
    public void AddRaccoonLandRequestProcessing_WhenCalledTwice_ThrowsInvalidOperation()
    {
        // Second call must fail fast so silent-orphaning (fresh registry replacing the previous one via
        // last-wins singleton resolution) cannot happen.
        var services = new ServiceCollection();
        services.AddRaccoonLandRequestProcessing(scanAssemblies: typeof(DoSomethingEndpoint).Assembly);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandRequestProcessing(scanAssemblies: typeof(DoSomethingEndpoint).Assembly));

        Assert.Contains("already been called", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRaccoonLandRequestProcessing_WhenCalledTwiceOnDifferentCollections_Succeeds()
    {
        // The one-call guard is scoped to a single IServiceCollection, not global — otherwise tests
        // that build many isolated containers would flake.
        var firstServices = new ServiceCollection();
        firstServices.AddRaccoonLandRequestProcessing(scanAssemblies: typeof(DoSomethingEndpoint).Assembly);

        var secondServices = new ServiceCollection();
        var ex = Record.Exception(() =>
            secondServices.AddRaccoonLandRequestProcessing(scanAssemblies: typeof(DoSomethingEndpoint).Assembly));

        Assert.Null(ex);
    }

    [Fact]
    public async Task AddRaccoonLandRequestProcessing_DoesNotOverwritePreRegisteredEndpoint()
    {
        // The scan uses TryAddScoped so a test override / decorator / custom-factory registration made
        // BEFORE the scan is preserved. The dispatcher must resolve the pre-registered instance, not a
        // scan-created one.
        var services = new ServiceCollection();
        var preRegistered = new GetSomethingEndpoint();
        services.AddScoped(_ => preRegistered);

        services.AddRaccoonLandRequestProcessing(scanAssemblies: typeof(DoSomethingEndpoint).Assembly);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var resolved = scope.ServiceProvider.GetRequiredService<GetSomethingEndpoint>();

        Assert.Same(preRegistered, resolved);

        // The dispatcher path resolves from the request scope too, so it also sees the pre-registered instance.
        var dispatcher = scope.ServiceProvider.GetRequiredService<IRequestDispatcher>();
        var response = await dispatcher.DispatchAsync(new GetSomethingQuery(), scope.ServiceProvider);
        Assert.Equal("value", response!.Result);
    }
}
