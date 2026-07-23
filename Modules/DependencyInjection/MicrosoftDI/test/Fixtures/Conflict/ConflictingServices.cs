using RaccoonLand.Modules.DependencyInjection.Abstractions;

namespace RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.ConflictingServices;

/// <summary>Two attributed implementations expose the same interface, which must fail the scan.</summary>
public interface IConflictContract;

[Service]
public sealed class FirstConflictingService : IConflictContract;

[Service]
public sealed class SecondConflictingService : IConflictContract;
