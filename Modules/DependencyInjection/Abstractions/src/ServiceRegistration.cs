namespace RaccoonLand.Modules.DependencyInjection.Abstractions;

/// <summary>
/// Describes how a <see cref="ServiceAttribute"/>-marked implementation type is registered in the container.
/// </summary>
public enum ServiceRegistration
{
    /// <summary>Registers the concrete type only.</summary>
    Self,

    /// <summary>Registers the type against a matching interface (for example <c>PersonService</c> → <c>IPersonService</c>).</summary>
    MatchingInterface,

    /// <summary>Registers the type against every interface it implements.</summary>
    ImplementedInterfaces,

    /// <summary>Registers the concrete type and every interface it implements.</summary>
    SelfAndImplementedInterfaces,
}
