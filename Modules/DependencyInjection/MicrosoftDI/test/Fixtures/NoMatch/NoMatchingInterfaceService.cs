using RaccoonLand.Modules.DependencyInjection.Abstractions;

namespace RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.InvalidMatchingInterface;

/// <summary>The implemented interface is not named <c>INoMatchingInterfaceService</c>, so MatchingInterface fails.</summary>
public interface IUnrelatedContract;

[Service(Registration = ServiceRegistration.MatchingInterface)]
public sealed class NoMatchingInterfaceService : IUnrelatedContract;
