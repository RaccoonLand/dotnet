using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware.Tests.DependencyInjection;

public sealed class ExceptionHandlingRegistrationTests
{
    [Fact]
    public void AddRaccoonLandExceptionHandling_RegistersOptionsAndMiddleware()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandExceptionHandling(o =>
            o.On<InvalidOperationException>((_, _) => Task.FromResult(true)));

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IOptions<ExceptionHandlingOptions>>());
        Assert.NotNull(provider.GetService<ExceptionHandlingMiddleware>());
        Assert.NotEmpty(provider.GetRequiredService<IOptions<ExceptionHandlingOptions>>().Value.Handlers);
    }

    [Fact]
    public void AddRaccoonLandExceptionHandling_RegistersMiddlewareAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandExceptionHandling();

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(ExceptionHandlingMiddleware));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }
}
