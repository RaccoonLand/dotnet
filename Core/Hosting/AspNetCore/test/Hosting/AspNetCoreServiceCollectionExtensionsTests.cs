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

    private sealed class CustomMapper : IPipelineResponseMapper
    {
        public IActionResult Map(PipelineResponse? response) => new EmptyResult();
    }
}
