namespace RaccoonLand.Modules.DependencyInjection.Abstractions;

/// <summary>
/// Describes how a <see cref="ServiceAttribute"/>-marked implementation type is registered in the container.
/// </summary>
public enum ServiceRegistration
{
    /// <summary>Registers the concrete type only.</summary>
    Self,

    /// <summary>Registers the type against a non-generic matching interface <c>I{ClassName}</c> (same namespace preferred).</summary>
    MatchingInterface,

    /// <summary>Registers the type against every interface it implements.</summary>
    ImplementedInterfaces,

    /// <summary>
    /// Registers the concrete type and every interface it implements as one registration.
    /// Scoped/Singleton resolves share one instance; Transient does not.
    /// </summary>
    SelfAndImplementedInterfaces,
}
