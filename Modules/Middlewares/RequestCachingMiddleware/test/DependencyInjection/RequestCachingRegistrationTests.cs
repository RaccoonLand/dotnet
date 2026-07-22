using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Support;

namespace RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.DependencyInjection;

public sealed class RequestCachingRegistrationTests
{
    [Fact]
    public void AddRaccoonLandRequestCaching_BindsDefaultSection()
    {
        var configuration = TestConfiguration.FromDictionary(new Dictionary<string, string?>
            {
                ["RequestCaching:Default:Duration"] = "00:03:00",
            });

        var services = new ServiceCollection();
        services.AddRaccoonLandRequestCaching(configuration);

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<RequestCachingOptions>>().Value;

        Assert.Equal(TimeSpan.FromMinutes(3), options.Default.Duration);
    }

    [Fact]
    public void AddRaccoonLandRequestCaching_BindsCustomSection()
    {
        var configuration = TestConfiguration.FromDictionary(new Dictionary<string, string?>
            {
                ["CustomCache:Default:Duration"] = "00:01:00",
            });

        var services = new ServiceCollection();
        services.AddRaccoonLandRequestCaching(configuration, sectionName: "CustomCache");

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<RequestCachingOptions>>().Value;

        Assert.Equal(TimeSpan.FromMinutes(1), options.Default.Duration);
    }

    [Fact]
    public void AddRaccoonLandRequestCaching_AppliesConfigureActionAfterBind()
    {
        var configuration = TestConfiguration.FromDictionary(new Dictionary<string, string?>
            {
                ["RequestCaching:Default:Duration"] = "00:03:00",
            });

        var services = new ServiceCollection();
        services.AddRaccoonLandRequestCaching(
            configuration,
            configure: o => o.Default.Duration = TimeSpan.FromSeconds(15));

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<RequestCachingOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(15), options.Default.Duration);
    }

    [Fact]
    public void AddRaccoonLandRequestCaching_CodeOnlyOverload_AppliesConfigure()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandRequestCaching(o =>
            o.Default.Duration = TimeSpan.FromSeconds(9));

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<RequestCachingOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(9), options.Default.Duration);
    }

    [Fact]
    public void AddRaccoonLandRequestCaching_RegistersMiddlewareAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandRequestCaching();

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(RequestCachingMiddleware));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddRaccoonLandRequestCaching_EnablesValidateOnStart()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandRequestCaching();

        Assert.Contains(
            services,
            d => d.ServiceType == typeof(IValidateOptions<RequestCachingOptions>));

        // ValidateOnStart is wired so invalid options fail when the options graph is built.
        services.Configure<RequestCachingOptions>(o => o.Default.Duration = TimeSpan.Zero);
        Assert.Throws<OptionsValidationException>(
            () => services.BuildServiceProvider().GetRequiredService<IOptions<RequestCachingOptions>>().Value);
    }

    [Fact]
    public void AddRaccoonLandRequestCaching_DoesNotRegisterIDistributedCache()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandRequestCaching();

        Assert.DoesNotContain(
            services,
            d => d.ServiceType == typeof(IDistributedCache));
    }
}
