using RaccoonLand.Modules.DependencyInjection.Abstractions;

namespace RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.AmbiguousMatchingInterface.NsA
{
    public interface IAmbiguousService;
}

namespace RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.AmbiguousMatchingInterface.NsB
{
    public interface IAmbiguousService;
}

namespace RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.AmbiguousMatchingInterface.Impl
{
    /// <summary>
    /// Implements two non-generic interfaces both named <c>IAmbiguousService</c> in different namespaces, and
    /// neither in this type's namespace, so the matching-interface resolution is ambiguous and must fail.
    /// </summary>
    [Service(Registration = ServiceRegistration.MatchingInterface)]
    public sealed class AmbiguousService : NsA.IAmbiguousService, NsB.IAmbiguousService;
}
