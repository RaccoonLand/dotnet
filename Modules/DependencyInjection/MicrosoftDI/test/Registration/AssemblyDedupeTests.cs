using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Support;
using RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.Valid;

namespace RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Registration;

public sealed class AssemblyDedupeTests
{
    [Fact]
    public void FromAssemblies_WithDuplicateAssembly_RegistersEachServiceOnce()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandDependencyInjectionFromAssemblies(
            FixtureAssemblies.Valid,
            FixtureAssemblies.Valid);

        Assert.Equal(1, services.Count(d => d.ServiceType == typeof(IIncludedPublicService)));
    }

    [Fact]
    public void MarkerOverload_WithMarkersFromSameAssembly_RegistersEachServiceOnce()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandDependencyInjection(typeof(IncludedPublicService), typeof(MultiInterfaceService));

        Assert.Equal(1, services.Count(d => d.ServiceType == typeof(IIncludedPublicService)));
        Assert.Equal(1, services.Count(d => d.ServiceType == typeof(IFirstService)));
    }
}
