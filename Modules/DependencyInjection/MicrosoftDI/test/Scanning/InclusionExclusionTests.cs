using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Support;
using RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.Valid;

namespace RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Scanning;

public sealed class InclusionExclusionTests
{
    private static IServiceCollection Scan()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandDependencyInjectionFromAssemblies(FixtureAssemblies.Valid);
        return services;
    }

    private static bool IsRegistered(IServiceCollection services, Type serviceType)
        => services.Any(d => d.ServiceType == serviceType);

    [Fact]
    public void PublicConcreteWithAttribute_IsRegistered()
        => Assert.True(IsRegistered(Scan(), typeof(IIncludedPublicService)));

    [Fact]
    public void InternalConcreteWithAttribute_IsRegistered()
        => Assert.True(IsRegistered(Scan(), typeof(IIncludedInternalService)));

    [Fact]
    public void NestedPublicAndInternalWithAttribute_AreRegistered()
    {
        var services = Scan();

        Assert.True(IsRegistered(services, typeof(INestedPublicService)));
        Assert.True(IsRegistered(services, typeof(INestedInternalService)));
    }

    [Fact]
    public void AbstractWithAttribute_IsNotRegistered()
        => Assert.False(IsRegistered(Scan(), typeof(IExcludedAbstractService)));

    [Fact]
    public void ConcreteWithoutAttribute_IsNotRegistered()
        => Assert.False(IsRegistered(Scan(), typeof(IExcludedNoAttributeService)));

    [Fact]
    public void OpenGenericWithAttribute_IsNotRegistered()
        => Assert.False(IsRegistered(Scan(), typeof(IExcludedGenericService<>)));

    [Fact]
    public void NestedPrivateAndProtectedWithAttribute_AreNotRegistered()
    {
        var services = Scan();

        Assert.False(IsRegistered(services, typeof(INestedPrivateService)));
        Assert.False(IsRegistered(services, typeof(INestedProtectedService)));
    }
}
