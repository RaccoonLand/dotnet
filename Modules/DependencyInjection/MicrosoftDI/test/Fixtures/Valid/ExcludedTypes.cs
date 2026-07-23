using RaccoonLand.Modules.DependencyInjection.Abstractions;

namespace RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.Valid;

// --- Types that MUST NOT be registered by the scan (exclusion rules). ---

public interface IExcludedAbstractService;

[Service]
public abstract class ExcludedAbstractService : IExcludedAbstractService;

public interface IExcludedNoAttributeService;

public sealed class ExcludedNoAttributeService : IExcludedNoAttributeService;

public interface IExcludedGenericService<T>;

[Service]
public sealed class ExcludedGenericService<T> : IExcludedGenericService<T>;

public interface INestedPrivateService;

public interface INestedProtectedService;

/// <summary>Non-sealed container so a <c>protected</c> nested type is legal without warnings.</summary>
public class ExcludedNestingContainer
{
    [Service]
    private sealed class NestedPrivateService : INestedPrivateService;

    [Service]
    protected sealed class NestedProtectedService : INestedProtectedService;
}
