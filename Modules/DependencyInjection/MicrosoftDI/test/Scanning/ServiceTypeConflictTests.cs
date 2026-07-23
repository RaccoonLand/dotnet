using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Support;

namespace RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Scanning;

public sealed class ServiceTypeConflictTests
{
    [Fact]
    public void TwoImplementationsExposingSameServiceType_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddRaccoonLandDependencyInjectionFromAssemblies(FixtureAssemblies.ConflictingServices));

        Assert.Contains("IConflictContract", ex.Message, StringComparison.Ordinal);
        Assert.Contains("FirstConflictingService", ex.Message, StringComparison.Ordinal);
        Assert.Contains("SecondConflictingService", ex.Message, StringComparison.Ordinal);
    }
}
