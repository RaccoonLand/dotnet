using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Support;
using RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.Valid;

namespace RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Registration;

public sealed class PublicApiGuardTests
{
    [Fact]
    public void AddRaccoonLandDependencyInjection_NullServices_Throws()
        => Assert.Throws<ArgumentNullException>(
            () => ((IServiceCollection)null!).AddRaccoonLandDependencyInjection(typeof(IncludedPublicService)));

    [Fact]
    public void AddRaccoonLandDependencyInjection_NullMarker_Throws()
        => Assert.Throws<ArgumentNullException>(
            () => new ServiceCollection().AddRaccoonLandDependencyInjection(null!));

    [Fact]
    public void AddRaccoonLandDependencyInjection_NullAdditionalMarkersArray_Throws()
        => Assert.Throws<ArgumentNullException>(
            () => new ServiceCollection().AddRaccoonLandDependencyInjection(typeof(IncludedPublicService), (Type[])null!));

    [Fact]
    public void AddRaccoonLandDependencyInjection_NullMarkerElement_Throws()
        => Assert.Throws<ArgumentNullException>(
            () => new ServiceCollection().AddRaccoonLandDependencyInjection(typeof(IncludedPublicService), [null!]));

    [Fact]
    public void AddRaccoonLandDependencyInjectionFromAssemblies_NullServices_Throws()
        => Assert.Throws<ArgumentNullException>(
            () => ((IServiceCollection)null!).AddRaccoonLandDependencyInjectionFromAssemblies(FixtureAssemblies.Valid));

    [Fact]
    public void AddRaccoonLandDependencyInjectionFromAssemblies_NullAssembliesArray_Throws()
        => Assert.Throws<ArgumentNullException>(
            () => new ServiceCollection().AddRaccoonLandDependencyInjectionFromAssemblies((Assembly[])null!));

    [Fact]
    public void AddRaccoonLandDependencyInjectionFromAssemblies_NoAssemblies_Throws()
        => Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AddRaccoonLandDependencyInjectionFromAssemblies());

    [Fact]
    public void AddRaccoonLandDependencyInjectionFromAssemblies_NullAssemblyElement_Throws()
        => Assert.Throws<ArgumentNullException>(
            () => new ServiceCollection().AddRaccoonLandDependencyInjectionFromAssemblies([null!]));
}
