using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Support;

namespace RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Registration;

public sealed class RegistrationLockTests
{
    [Fact]
    public void SecondCall_AfterFailedScan_ThrowsAlreadyRegistered()
    {
        var services = new ServiceCollection();

        // The marker is claimed before validation runs, so even a failed scan locks the collection.
        Assert.Throws<InvalidOperationException>(
            () => services.AddRaccoonLandDependencyInjectionFromAssemblies(FixtureAssemblies.InvalidMatchingInterface));

        var second = Assert.Throws<InvalidOperationException>(
            () => services.AddRaccoonLandDependencyInjectionFromAssemblies(FixtureAssemblies.InvalidMatchingInterface));

        Assert.Contains("already called", second.Message, StringComparison.Ordinal);
    }
}
