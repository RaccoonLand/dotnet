using RaccoonLand.Modules.DependencyInjection.Abstractions;

namespace RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.Valid;

// --- One implementation per registration strategy / lifetime, with unique service contracts. ---

public interface ISelfOnlyService;

[Service(Registration = ServiceRegistration.Self, Lifetime = ServiceLifetime.Singleton)]
public sealed class SelfOnlyService : ISelfOnlyService;

public interface IMatchingService;

public interface IMatchingExtraMarker;

[Service(Registration = ServiceRegistration.MatchingInterface)]
public sealed class MatchingService : IMatchingService, IMatchingExtraMarker;

public interface IFirstService;

public interface ISecondService;

[Service(Registration = ServiceRegistration.ImplementedInterfaces)]
public sealed class MultiInterfaceService : IFirstService, ISecondService;

public interface ISingletonSelfAndInterfaces;

[Service(Registration = ServiceRegistration.SelfAndImplementedInterfaces, Lifetime = ServiceLifetime.Singleton)]
public sealed class SingletonSelfAndInterfaces : ISingletonSelfAndInterfaces;

public interface IScopedSelfAndInterfaces;

[Service(Registration = ServiceRegistration.SelfAndImplementedInterfaces, Lifetime = ServiceLifetime.Scoped)]
public sealed class ScopedSelfAndInterfaces : IScopedSelfAndInterfaces;

public interface ITransientSelfAndInterfaces;

[Service(Registration = ServiceRegistration.SelfAndImplementedInterfaces, Lifetime = ServiceLifetime.Transient)]
public sealed class TransientSelfAndInterfaces : ITransientSelfAndInterfaces;
