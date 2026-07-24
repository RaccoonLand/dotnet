using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Core.Hosting.AspNetCore.Hosting;
using RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;
using RaccoonLand.Core.Hosting.AspNetCore.PipelineResponseMapping;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using ExecutionContextType = RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext.HttpExecutionContext;

namespace RaccoonLand.Core.Hosting.AspNetCore.Tests.Hosting;

public sealed class AspNetCoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRaccoonLandAspNetCore_RegistersDefaultMapper_WhenMissing()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandAspNetCore();

        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IPipelineResponseMapper>();

        Assert.IsType<DefaultPipelineResponseMapper>(mapper);
    }

    [Fact]
    public void AddRaccoonLandAspNetCore_DoesNotOverrideCustomMapper()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPipelineResponseMapper, CustomMapper>();
        services.AddRaccoonLandAspNetCore();

        var provider = services.BuildServiceProvider();

        Assert.IsType<CustomMapper>(provider.GetRequiredService<IPipelineResponseMapper>());
    }

    [Fact]
    public void AddRaccoonLandAspNetCore_WithConfiguration_RegistersHttpExecutionContext()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HttpExecutionContext:UserIdClaim"] = "sub",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddRaccoonLandAspNetCore(configuration: config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<ExecutionContextType>(
            scope.ServiceProvider.GetRequiredService<ICurrentExecutionContext>());
        Assert.IsType<ExecutionContextType>(
            scope.ServiceProvider.GetRequiredService<ExecutionContextType>());
    }

    [Fact]
    public void AddRaccoonLandAspNetCore_WithConfigureActionOnly_RegistersHttpExecutionContext()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandAspNetCore(configureExecutionContext: o => o.UserIdClaim = "oid");

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<ExecutionContextType>(
            scope.ServiceProvider.GetRequiredService<ICurrentExecutionContext>());
    }

    [Fact]
    public void AddRaccoonLandAspNetCore_Throws_WhenServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            AspNetCoreServiceCollectionExtensions.AddRaccoonLandAspNetCore(null!));
    }

    [Fact]
    public void AddRaccoonLandAspNetCore_WithConfiguration_ReplacesPreviouslyRegisteredExecutionContext()
    {
        // Regression: this module is a host adapter and its ICurrentExecutionContext registration is
        // authoritative. A prior registration (e.g. NullCurrentExecutionContext added by another
        // module) must NOT survive — otherwise the HTTP middleware would populate HttpExecutionContext
        // while consumers keep injecting the older, unpopulated implementation.
        var services = new ServiceCollection();
        services.AddScoped<ICurrentExecutionContext, StubExecutionContext>();

        services.AddRaccoonLandAspNetCore(
            configureExecutionContext: o => o.UserIdClaim = "sub");

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<ExecutionContextType>(
            scope.ServiceProvider.GetRequiredService<ICurrentExecutionContext>());
        Assert.IsType<ExecutionContextType>(
            scope.ServiceProvider.GetRequiredService<ExecutionContextType>());

        // And there is only one descriptor left, so future GetServices() enumeration is unambiguous.
        Assert.Single(services, d => d.ServiceType == typeof(ICurrentExecutionContext));
    }

    [Fact]
    public void AddRaccoonLandAspNetCore_WithConfiguration_HttpExecutionContextAndInterface_ResolveSameInstance()
    {
        // The interface must delegate to the same scoped concrete instance so the middleware's
        // Populate() actually affects what consumers see.
        var services = new ServiceCollection();
        services.AddRaccoonLandAspNetCore(configureExecutionContext: _ => { });

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var concrete = scope.ServiceProvider.GetRequiredService<ExecutionContextType>();
        var abstraction = scope.ServiceProvider.GetRequiredService<ICurrentExecutionContext>();

        Assert.Same(concrete, abstraction);
    }

    private sealed class CustomMapper : IPipelineResponseMapper
    {
        public IActionResult Map(PipelineResponse? response) => new EmptyResult();
    }

    private sealed class StubExecutionContext : ICurrentExecutionContext
    {
        public bool IsAvailable => false;

        public string? UserId => null;

        public string? TenantId => null;

        public string? CorrelationId => null;
    }
}
