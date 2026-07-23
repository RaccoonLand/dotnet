using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Observability.Instrumentation.Configuration;
using RaccoonLand.Modules.Observability.Instrumentation.Diagnostics;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.DependencyInjection;

public sealed class InstrumentationRegistrationTests
{
    [Fact]
    public void AddInstrumentation_RegistersMiddlewareAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandPipelineInstrumentation();

        var descriptor = services.Single(d => d.ServiceType == typeof(PipelineInstrumentationMiddleware));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddInstrumentation_RegistersValidatorAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandPipelineInstrumentation();

        var descriptor = Assert.Single(services,
            d => d.ServiceType == typeof(IValidateOptions<InstrumentationOptions>)
                 && d.ImplementationType == typeof(InstrumentationOptionsValidator));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddInstrumentation_CodeOverload_AppliesConfiguredToggles()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandPipelineInstrumentation(options =>
        {
            options.EnableTracing = false;
            options.RequestNameInMetrics = RequestNameMetricTag.Name;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<InstrumentationOptions>>().Value;

        Assert.False(options.EnableTracing);
        Assert.Equal(RequestNameMetricTag.Name, options.RequestNameInMetrics);
    }

    [Fact]
    public void AddInstrumentation_ConfigurationOverload_BindsSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:Instrumentation:EnableMetrics"] = "false",
                ["Observability:Instrumentation:RequestNameInMetrics"] = "None",
            })
            .Build();
        var services = new ServiceCollection();

        services.AddRaccoonLandPipelineInstrumentation(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<InstrumentationOptions>>().Value;

        Assert.False(options.EnableMetrics);
        Assert.Equal(RequestNameMetricTag.None, options.RequestNameInMetrics);
    }

    [Fact]
    public void AddInstrumentation_WhenValidationFails_ThrowsOnOptionsResolution()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:Instrumentation:RequestNameInMetrics"] = "999",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddRaccoonLandPipelineInstrumentation(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<InstrumentationOptions>>().Value);
    }

    [Fact]
    public void AddInstrumentation_WhenCalledTwice_DoesNotDuplicateServices()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandPipelineInstrumentation();
        services.AddRaccoonLandPipelineInstrumentation();

        Assert.Equal(1, services.Count(d => d.ServiceType == typeof(PipelineInstrumentationMiddleware)));
        Assert.Equal(1, services.Count(
            d => d.ServiceType == typeof(IValidateOptions<InstrumentationOptions>)
                 && d.ImplementationType == typeof(InstrumentationOptionsValidator)));
    }

    [Fact]
    public void AddInstrumentation_CodeOverload_WhenServicesNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => InstrumentationServiceCollectionExtensions.AddRaccoonLandPipelineInstrumentation(null!));
    }

    [Fact]
    public void AddInstrumentation_ConfigurationOverload_WhenServicesNull_Throws()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(
            () => InstrumentationServiceCollectionExtensions.AddRaccoonLandPipelineInstrumentation(null!, configuration));
    }

    [Fact]
    public void AddInstrumentation_ConfigurationOverload_WhenConfigurationNull_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(
            () => services.AddRaccoonLandPipelineInstrumentation((IConfiguration)null!));
    }
}
