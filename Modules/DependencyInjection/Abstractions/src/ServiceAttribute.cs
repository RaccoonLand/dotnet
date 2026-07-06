namespace RaccoonLand.Modules.DependencyInjection.Abstractions;

/// <summary>
/// Marks a class for automatic DI registration when
/// <c>AddRaccoonLandDependencyInjection</c> scans the containing assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ServiceAttribute : Attribute
{
    /// <summary>Container lifetime. Defaults to <see cref="ServiceLifetime.Scoped"/>.</summary>
    public ServiceLifetime Lifetime { get; init; } = ServiceLifetime.Scoped;

    /// <summary>Registration strategy. Defaults to <see cref="ServiceRegistration.ImplementedInterfaces"/>.</summary>
    public ServiceRegistration Registration { get; init; } = ServiceRegistration.ImplementedInterfaces;
}
