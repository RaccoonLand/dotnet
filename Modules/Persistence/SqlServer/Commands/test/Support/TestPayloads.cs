namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

/// <summary>Plain payload whose CLR <c>Name</c> is used as the outbox EventType fallback.</summary>
public sealed class SamplePayload
{
    public string Value { get; init; } = string.Empty;
}
