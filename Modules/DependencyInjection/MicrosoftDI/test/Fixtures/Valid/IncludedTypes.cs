using RaccoonLand.Modules.DependencyInjection.Abstractions;

namespace RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.Valid;

// --- Types that MUST be registered by the scan (inclusion rules). ---

public interface IIncludedPublicService;

[Service]
public sealed class IncludedPublicService : IIncludedPublicService;

public interface IIncludedInternalService;

[Service]
internal sealed class IncludedInternalService : IIncludedInternalService;

public interface INestedPublicService;

public interface INestedInternalService;

/// <summary>Container without a <see cref="ServiceAttribute"/>; only its allowed nested types are registered.</summary>
public sealed class IncludedNestingContainer
{
    [Service]
    public sealed class NestedPublicService : INestedPublicService;

    [Service]
    internal sealed class NestedInternalService : INestedInternalService;
}
