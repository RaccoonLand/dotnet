using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.Persistence.Outbox.Abstraction;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Interceptors;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.DependencyInjection;

public sealed class OutboxRegistrationTests
{
    [Fact]
    public void AddRaccoonLandOutbox_WhenServicesNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => OutboxServiceCollectionExtensions.AddRaccoonLandOutbox(null!));
    }

    [Fact]
    public void AddRaccoonLandOutbox_RegistersRegistryAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandOutbox();

        Assert.Equal(ServiceLifetime.Singleton, LifetimeOf(services, typeof(OutboxChannelRegistry)));
        Assert.Equal(ServiceLifetime.Singleton, LifetimeOf(services, typeof(IOutboxChannelRegistry)));
    }

    [Fact]
    public void AddRaccoonLandOutbox_RegistersWriterAndInterceptorAsScoped()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandOutbox();

        Assert.Equal(ServiceLifetime.Scoped, LifetimeOf(services, typeof(OutboxWriter)));
        Assert.Equal(ServiceLifetime.Scoped, LifetimeOf(services, typeof(IOutboxWriter)));
        Assert.Equal(ServiceLifetime.Scoped, LifetimeOf(services, typeof(OutboxWriterSaveChangesInterceptor)));
    }

    [Fact]
    public void AddRaccoonLandOutbox_WhenCalledTwice_DoesNotDuplicateServices()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandOutbox();
        services.AddRaccoonLandOutbox();

        Assert.Equal(1, CountOf(services, typeof(OutboxWriter)));
        Assert.Equal(1, CountOf(services, typeof(IOutboxWriter)));
        Assert.Equal(1, CountOf(services, typeof(OutboxWriterSaveChangesInterceptor)));
        Assert.Equal(1, CountOf(services, typeof(IOutboxChannelRegistry)));
    }

    [Fact]
    public void AddRaccoonLandOutbox_WhenCustomRegistryPreregistered_DoesNotOverwriteIt()
    {
        var custom = new OutboxChannelRegistry();
        var services = new ServiceCollection();
        services.AddSingleton<IOutboxChannelRegistry>(custom);

        services.AddRaccoonLandOutbox();

        using var provider = services.BuildServiceProvider();
        Assert.Same(custom, provider.GetRequiredService<IOutboxChannelRegistry>());
    }

    [Fact]
    public void AddRaccoonLandOutbox_WhenChannelTypeRegistered_AppliesTableToRegistry()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandOutbox<ITestOutbox>(options => options.Table = "TestOutbox");

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IOutboxChannelRegistry>();
        Assert.Equal("TestOutbox", registry.Get<ITestOutbox>()!.Table);
    }

    [Fact]
    public void Enqueue_WhenChannelNotRegistered_FailsDetectably()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandOutbox();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();

        Assert.Throws<InvalidOperationException>(
            () => writer.Enqueue<ITestOutbox>(new SamplePayload()));
    }

    private static ServiceLifetime LifetimeOf(IServiceCollection services, Type serviceType)
        => services.Single(descriptor => descriptor.ServiceType == serviceType).Lifetime;

    private static int CountOf(IServiceCollection services, Type serviceType)
        => services.Count(descriptor => descriptor.ServiceType == serviceType);
}
