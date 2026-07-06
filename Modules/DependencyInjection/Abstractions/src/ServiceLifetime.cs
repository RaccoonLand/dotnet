namespace RaccoonLand.Modules.DependencyInjection.Abstractions;

/// <summary>
/// DI lifetime for services registered through <see cref="ServiceAttribute"/>.
/// </summary>
public enum ServiceLifetime
{
    Singleton,
    Scoped,
    Transient,
}
