using RaccoonLand.Modules.Persistence.Outbox.Abstraction;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

/// <summary>A registered outbox channel marker used by writer/registry tests.</summary>
public interface ITestOutbox : IOutbox;

/// <summary>A second channel marker to prove per-channel batching and isolation.</summary>
public interface IOtherOutbox : IOutbox;

/// <summary>A channel marker that is intentionally never registered.</summary>
public interface IUnregisteredOutbox : IOutbox;
