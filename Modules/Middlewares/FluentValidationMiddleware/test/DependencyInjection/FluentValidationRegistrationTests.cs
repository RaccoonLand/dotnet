using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace RaccoonLand.Modules.Middlewares.FluentValidationMiddleware.Tests.DependencyInjection;

public sealed class FluentValidationRegistrationTests
{
    [Fact]
    public void AddRaccoonLandFluentValidation_WhenServicesNull_Throws()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddRaccoonLandFluentValidation());
    }

    [Fact]
    public void AddRaccoonLandFluentValidation_RegistersMiddlewareAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandFluentValidation();

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(FluentValidationMiddleware));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddRaccoonLandFluentValidation_DoesNotRegisterValidators()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandFluentValidation();

        Assert.DoesNotContain(
            services,
            d => d.ServiceType.IsGenericType
                 && d.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>));
    }
}
