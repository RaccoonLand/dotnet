namespace RaccoonLand.Modules.Persistence.Outbox.Abstraction;

/// <summary>
/// Marker for a logical outbox channel. Implement or inherit this interface to identify which outbox table
/// and configuration a message belongs to. The interface carries no members; routing is resolved at
/// registration time via <c>AddRaccoonLandOutbox&lt;TOutbox&gt;()</c>.
/// </summary>
public interface IOutbox;
