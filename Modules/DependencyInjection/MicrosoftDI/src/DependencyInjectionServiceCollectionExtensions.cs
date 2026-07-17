using System.Reflection;
using RaccoonLand.Modules.DependencyInjection.Abstractions;
using Scrutor;
using RaccoonLandServiceLifetime = RaccoonLand.Modules.DependencyInjection.Abstractions.ServiceLifetime;
using RaccoonLandServiceRegistration = RaccoonLand.Modules.DependencyInjection.Abstractions.ServiceRegistration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers classes marked with <see cref="ServiceAttribute"/> using Scrutor assembly scanning.
/// Call <see cref="AddRaccoonLandDependencyInjection(IServiceCollection,Type,Type[])"/> or
/// <see cref="AddRaccoonLandDependencyInjectionFromAssemblies"/> <b>once</b> at startup.
/// </summary>
public static class DependencyInjectionServiceCollectionExtensions
{
    /// <summary>
    /// Scans assemblies containing the given marker types for concrete classes decorated with
    /// <see cref="ServiceAttribute"/> and registers them.
    /// </summary>
    /// <param name="services">The service collection. Must not already have been scanned by this extension.</param>
    /// <param name="assemblyMarker">A type whose assembly is scanned.</param>
    /// <param name="additionalAssemblyMarkers">
    /// Optional extra marker types. Must not be <see langword="null"/> (omit the argument for none).
    /// </param>
    public static IServiceCollection AddRaccoonLandDependencyInjection(
        this IServiceCollection services,
        Type assemblyMarker,
        params Type[] additionalAssemblyMarkers)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblyMarker);
        ArgumentNullException.ThrowIfNull(additionalAssemblyMarkers);

        foreach (var marker in additionalAssemblyMarkers)
        {
            ArgumentNullException.ThrowIfNull(marker);
        }

        var assemblies = additionalAssemblyMarkers
            .Prepend(assemblyMarker)
            .Select(static marker => marker.Assembly)
            .ToArray();

        return AddRaccoonLandDependencyInjectionFromAssemblies(services, assemblies);
    }

    /// <summary>
    /// Scans <paramref name="assemblies"/> for concrete classes decorated with <see cref="ServiceAttribute"/>
    /// and registers them according to the attribute's lifetime and registration strategy.
    /// At least one assembly is required.
    /// </summary>
    /// <param name="services">The service collection. Must not already have been scanned by this extension.</param>
    /// <param name="assemblies">Assemblies to scan. Must not be null or empty; null elements are rejected.</param>
    public static IServiceCollection AddRaccoonLandDependencyInjectionFromAssemblies(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        if (assemblies.Length == 0)
        {
            throw new ArgumentException(
                "At least one assembly is required. Pass assemblies explicitly, or use " +
                $"{nameof(AddRaccoonLandDependencyInjection)} with a marker type.",
                nameof(assemblies));
        }

        foreach (var assembly in assemblies)
        {
            ArgumentNullException.ThrowIfNull(assembly);
        }

        // Claim the collection before scanning so a mid-failure cannot be retried and silently
        // double-register. A failed call leaves the collection unusable for this extension —
        // create a new IServiceCollection and start over.
        EnsureNotAlreadyRegistered(services);
        services.AddSingleton<RaccoonLandDependencyInjectionRegistrationMarker>();

        var uniqueAssemblies = assemblies.Distinct().ToArray();

        ValidateMatchingInterfaceContracts(uniqueAssemblies);
        ValidateNoServiceTypeConflicts(uniqueAssemblies);

        foreach (var lifetime in Enum.GetValues<RaccoonLandServiceLifetime>())
        {
            foreach (var registration in Enum.GetValues<RaccoonLandServiceRegistration>())
            {
                RegisterAttributedServices(services, uniqueAssemblies, lifetime, registration);
            }
        }

        return services;
    }

    private static void EnsureNotAlreadyRegistered(IServiceCollection services)
    {
        if (services.Any(static d => d.ServiceType == typeof(RaccoonLandDependencyInjectionRegistrationMarker)))
        {
            throw new InvalidOperationException(
                $"{nameof(AddRaccoonLandDependencyInjection)} / {nameof(AddRaccoonLandDependencyInjectionFromAssemblies)} " +
                $"was already called on this {nameof(IServiceCollection)} (including a previous failed attempt). " +
                "Call it once at startup with the full set of assemblies or marker types. " +
                "After a failure, discard this collection and create a new one.");
        }
    }

    private static void ValidateMatchingInterfaceContracts(Assembly[] assemblies)
    {
        foreach (var type in EnumerateAttributedTypes(assemblies))
        {
            var attribute = type.GetCustomAttribute<ServiceAttribute>(inherit: false)!;
            if (attribute.Registration != RaccoonLandServiceRegistration.MatchingInterface)
            {
                continue;
            }

            if (!TryResolveMatchingInterface(type, out _))
            {
                throw new InvalidOperationException(BuildMatchingInterfaceError(type));
            }
        }
    }

    /// <summary>
    /// Fail-fast when two different attributed implementations would expose the same service type.
    /// </summary>
    private static void ValidateNoServiceTypeConflicts(Assembly[] assemblies)
    {
        var owners = new Dictionary<Type, Type>();

        foreach (var implementation in EnumerateAttributedTypes(assemblies))
        {
            var attribute = implementation.GetCustomAttribute<ServiceAttribute>(inherit: false)!;

            foreach (var serviceType in GetExposedServiceTypes(implementation, attribute.Registration))
            {
                if (owners.TryGetValue(serviceType, out var existing) && existing != implementation)
                {
                    throw new InvalidOperationException(
                        $"Service type '{serviceType.FullName}' would be registered by both " +
                        $"'{existing.FullName}' and '{implementation.FullName}'. " +
                        "Keep one attributed implementation per service contract.");
                }

                owners[serviceType] = implementation;
            }
        }
    }

    private static IEnumerable<Type> EnumerateAttributedTypes(Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type is not { IsClass: true, IsAbstract: false }
                    || type.IsGenericTypeDefinition
                    || !IsPublicOrInternal(type))
                {
                    continue;
                }

                if (type.GetCustomAttribute<ServiceAttribute>(inherit: false) is null)
                {
                    continue;
                }

                yield return type;
            }
        }
    }

    private static IEnumerable<Type> GetExposedServiceTypes(
        Type implementation,
        RaccoonLandServiceRegistration registration)
    {
        return registration switch
        {
            RaccoonLandServiceRegistration.Self => [implementation],
            RaccoonLandServiceRegistration.MatchingInterface =>
                TryResolveMatchingInterface(implementation, out var matching) && matching is not null
                    ? [matching]
                    : [],
            RaccoonLandServiceRegistration.ImplementedInterfaces =>
                GetRegisterableInterfaces(implementation),
            RaccoonLandServiceRegistration.SelfAndImplementedInterfaces =>
                GetRegisterableInterfaces(implementation).Prepend(implementation),
            _ => [implementation],
        };
    }

    private static IEnumerable<Type> GetRegisterableInterfaces(Type implementation)
    {
        foreach (var iface in implementation.GetInterfaces())
        {
            if (iface == typeof(IDisposable) || iface == typeof(IAsyncDisposable))
            {
                continue;
            }

            if (iface.IsGenericType && iface.ContainsGenericParameters)
            {
                continue;
            }

            yield return iface;
        }
    }

    private static void RegisterAttributedServices(
        IServiceCollection services,
        Assembly[] assemblies,
        RaccoonLandServiceLifetime lifetime,
        RaccoonLandServiceRegistration registration)
    {
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
                // Shared instance for Scoped/Singleton; Transient still creates a new object per resolve.
                RaccoonLandServiceRegistration.SelfAndImplementedInterfaces => selector.AsSelfWithInterfaces(),
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
        if (type is not { IsClass: true, IsAbstract: false } || type.IsGenericTypeDefinition)
        {
            return false;
        }

        if (!IsPublicOrInternal(type))
        {
            return false;
        }

        var attribute = type.GetCustomAttribute<ServiceAttribute>(inherit: false);
        return attribute is not null
            && attribute.Lifetime == lifetime
            && attribute.Registration == registration;
    }

    /// <summary>
    /// Resolves <c>I{ClassName}</c> for non-generic types: prefers same namespace, requires a unique
    /// non-generic interface with that simple name when namespaces differ.
    /// </summary>
    private static bool TryResolveMatchingInterface(Type type, out Type? matchingInterface)
    {
        matchingInterface = null;

        // Scrutor matches on simple name (IClassName). Generics (`Name`1`) are not supported here.
        if (type.IsGenericType)
        {
            return false;
        }

        var expectedName = "I" + type.Name;
        var candidates = type.GetInterfaces()
            .Where(i => !i.IsGenericType && string.Equals(i.Name, expectedName, StringComparison.Ordinal))
            .ToArray();

        if (candidates.Length == 0)
        {
            return false;
        }

        var sameNamespace = candidates
            .Where(i => string.Equals(i.Namespace, type.Namespace, StringComparison.Ordinal))
            .ToArray();

        if (sameNamespace.Length == 1)
        {
            matchingInterface = sameNamespace[0];
            return true;
        }

        if (sameNamespace.Length > 1)
        {
            return false;
        }

        if (candidates.Length == 1)
        {
            matchingInterface = candidates[0];
            return true;
        }

        return false;
    }

    private static string BuildMatchingInterfaceError(Type type)
    {
        if (type.IsGenericType)
        {
            return $"Type '{type.FullName}' is marked with {nameof(ServiceRegistration.MatchingInterface)}, " +
                   "but open/closed generic implementations are not supported for that strategy. " +
                   $"Use {nameof(ServiceRegistration.ImplementedInterfaces)} or {nameof(ServiceRegistration.Self)} instead.";
        }

        var expectedName = "I" + type.Name;
        return $"Type '{type.FullName}' is marked with {nameof(ServiceRegistration.MatchingInterface)} " +
               $"but does not implement a unique non-generic interface named '{expectedName}' " +
               "(prefer the same namespace as the class). " +
               "Rename the interface, disambiguate namespaces, change the registration strategy, or fix the typo.";
    }

    private static bool IsPublicOrInternal(Type type)
    {
        if (type.IsNested)
        {
            return type.IsNestedPublic || type.IsNestedAssembly;
        }

        return type.IsPublic || type.IsNotPublic;
    }

    /// <summary>
    /// Marker so registration runs at most once per collection (including after a failed attempt).
    /// </summary>
    private sealed class RaccoonLandDependencyInjectionRegistrationMarker;
}
