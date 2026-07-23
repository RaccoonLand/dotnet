using Microsoft.Extensions.DependencyInjection;

namespace RaccoonLand.Modules.Security.Authorization.Middlewares.Tests.DependencyInjection;

public sealed class AuthorizationMiddlewareRegistrationTests
{
    [Fact]
    public void AddRaccoonLandAuthorization_RegistersMiddlewareAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandAuthorization();

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(AuthorizationMiddleware));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddRaccoonLandAuthorization_WhenCalledTwice_DoesNotDuplicateRegistration()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandAuthorization();
        services.AddRaccoonLandAuthorization();

        Assert.Single(services, d => d.ServiceType == typeof(AuthorizationMiddleware));
    }

    [Fact]
    public void AddRaccoonLandAuthorization_WhenServicesNull_Throws()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddRaccoonLandAuthorization());
    }
}
