namespace RaccoonLand.Modules.Persistence.Outbox.Abstraction;

/// <summary>
/// Marker for a logical outbox channel. Implement or inherit this interface to identify which outbox
/// destination a message belongs to. The interface carries no members; routing is resolved by the
/// <strong>implementation</strong> at registration time (for example SQL Server's
/// <c>AddRaccoonLandOutbox&lt;TOutbox&gt;()</c>).
/// </summary>
/// <remarks>
/// Registration semantics (duplicate register, unknown channel) are defined by the implementation.
/// The abstraction expects unregistered channels to fail on <see cref="IOutboxWriter.Enqueue{TOutbox}"/>.
/// </remarks>
public interface IOutbox;
