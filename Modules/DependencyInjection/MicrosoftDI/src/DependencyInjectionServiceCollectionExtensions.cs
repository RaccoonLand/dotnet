using System.Reflection;
using RaccoonLand.Modules.DependencyInjection.Abstractions;
using Scrutor;
using RaccoonLandServiceLifetime = RaccoonLand.Modules.DependencyInjection.Abstractions.ServiceLifetime;
using RaccoonLandServiceRegistration = RaccoonLand.Modules.DependencyInjection.Abstractions.ServiceRegistration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers classes marked with <see cref="ServiceAttribute"/> using Scrutor assembly scanning.
/// </summary>
public static class DependencyInjectionServiceCollectionExtensions
{
    /// <summary>
    /// Scans <paramref name="assemblies"/> for concrete classes decorated with <see cref="ServiceAttribute"/>
    /// and registers them according to the attribute's lifetime and registration strategy.
    /// When no assemblies are supplied, the calling assembly is scanned.
    /// </summary>
    public static IServiceCollection AddRaccoonLandDependencyInjection(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        foreach (var lifetime in Enum.GetValues<RaccoonLandServiceLifetime>())
        {
            foreach (var registration in Enum.GetValues<RaccoonLandServiceRegistration>())
            {
                RegisterAttributedServices(services, assemblies, lifetime, registration);
            }
        }

        return services;
    }

    private static void RegisterAttributedServices(
        IServiceCollection services,
        Assembly[] assemblies,
        RaccoonLandServiceLifetime lifetime,
        RaccoonLandServiceRegistration registration)
    {
        if (registration == RaccoonLandServiceRegistration.SelfAndImplementedInterfaces)
        {
            RegisterGroup(services, assemblies, lifetime, registration, RaccoonLandServiceRegistration.Self);
            RegisterGroup(services, assemblies, lifetime, registration, RaccoonLandServiceRegistration.ImplementedInterfaces);
            return;
        }

        RegisterGroup(services, assemblies, lifetime, registration, registration);
    }

    private static void RegisterGroup(
        IServiceCollection services,
        Assembly[] assemblies,
        RaccoonLandServiceLifetime lifetime,
        RaccoonLandServiceRegistration attributeRegistration,
        RaccoonLandServiceRegistration scrutorRegistration)
    {
        services.Scan(scan =>
        {
            var selector = scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.Where(type => Matches(type, lifetime, attributeRegistration)));

            var lifetimeSelector = scrutorRegistration switch
            {
                RaccoonLandServiceRegistration.Self => selector.AsSelf(),
                RaccoonLandServiceRegistration.MatchingInterface => selector.AsMatchingInterface(),
                RaccoonLandServiceRegistration.ImplementedInterfaces => selector.AsImplementedInterfaces(),
                _ => selector.AsSelf(),
            };

            switch (lifetime)
            {
                case RaccoonLandServiceLifetime.Singleton:
                    lifetimeSelector.WithSingletonLifetime();
                    break;
                case RaccoonLandServiceLifetime.Scoped:
                    lifetimeSelector.WithScopedLifetime();
                    break;
                case RaccoonLandServiceLifetime.Transient:
                    lifetimeSelector.WithTransientLifetime();
                    break;
            }
        });
    }

    private static bool Matches(
        Type type,
        RaccoonLandServiceLifetime lifetime,
        RaccoonLandServiceRegistration registration)
    {
        if (type is not { IsClass: true, IsAbstract: false })
        {
            return false;
        }

        var attribute = type.GetCustomAttribute<ServiceAttribute>(inherit: false);
        return attribute is not null
            && attribute.Lifetime == lifetime
            && attribute.Registration == registration;
    }
}
