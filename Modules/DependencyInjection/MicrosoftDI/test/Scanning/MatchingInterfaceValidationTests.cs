using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Support;
using RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.Valid;

namespace RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Scanning;

public sealed class MatchingInterfaceValidationTests
{
    [Fact]
    public void MatchingInterface_WithoutCorrespondingInterface_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddRaccoonLandDependencyInjectionFromAssemblies(FixtureAssemblies.InvalidMatchingInterface));

        Assert.Contains("MatchingInterface", ex.Message, StringComparison.Ordinal);
        Assert.Contains("INoMatchingInterfaceService", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MatchingInterface_WithAmbiguousCandidates_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddRaccoonLandDependencyInjectionFromAssemblies(FixtureAssemblies.AmbiguousMatchingInterface));

        Assert.Contains("IAmbiguousService", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MatchingInterface_WithValidInterface_RegistersOnlyThatInterface()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandDependencyInjectionFromAssemblies(FixtureAssemblies.Valid);

        Assert.Equal(1, services.Count(d => d.ServiceType == typeof(IMatchingService)));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IMatchingExtraMarker));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(MatchingService));
    }
}
