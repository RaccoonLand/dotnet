using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Support;
using RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.Valid;

namespace RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Resolution;

public sealed class LifetimeAndRegistrationResolveTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandDependencyInjectionFromAssemblies(FixtureAssemblies.Valid);
        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public void Self_ResolvesConcreteOnly()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        Assert.NotNull(services.GetService<SelfOnlyService>());
        Assert.Null(services.GetService<ISelfOnlyService>());
    }

    [Fact]
    public void MatchingInterface_ResolvesMatchingInterfaceOnly()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        Assert.NotNull(services.GetService<IMatchingService>());
        Assert.Null(services.GetService<MatchingService>());
        Assert.Null(services.GetService<IMatchingExtraMarker>());
    }

    [Fact]
    public void ImplementedInterfaces_ResolvesInterfacesButNotSelf()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        Assert.NotNull(services.GetService<IFirstService>());
        Assert.NotNull(services.GetService<ISecondService>());
        Assert.Null(services.GetService<MultiInterfaceService>());
    }

    [Fact]
    public void SelfAndImplementedInterfaces_Singleton_SharesOneInstance()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        var self = services.GetRequiredService<SingletonSelfAndInterfaces>();
        var iface = services.GetRequiredService<ISingletonSelfAndInterfaces>();

        Assert.Same(self, iface);
    }

    [Fact]
    public void SelfAndImplementedInterfaces_Scoped_SharesOneInstanceWithinScope()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        var self = services.GetRequiredService<ScopedSelfAndInterfaces>();
        var iface = services.GetRequiredService<IScopedSelfAndInterfaces>();

        Assert.Same(self, iface);
    }

    [Fact]
    public void SelfAndImplementedInterfaces_Transient_CreatesSeparateInstances()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        var self = services.GetRequiredService<TransientSelfAndInterfaces>();
        var iface = services.GetRequiredService<ITransientSelfAndInterfaces>();
        var ifaceAgain = services.GetRequiredService<ITransientSelfAndInterfaces>();

        Assert.NotSame(self, iface);
        Assert.NotSame(iface, ifaceAgain);
    }
}
