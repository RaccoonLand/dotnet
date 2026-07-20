namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Outcome of <see cref="IInboxStore.TryClaimAsync"/>. When <see cref="Result"/> is
/// <see cref="InboxClaimResult.Claimed"/>, <see cref="Token"/> is required for
/// <see cref="IInboxStore.MarkProcessedAsync"/> / <see cref="IInboxStore.ReleaseAsync"/>.
/// </summary>
public sealed class InboxClaimAttempt
{
    public required InboxClaimResult Result { get; init; }

    /// <summary>Present only when <see cref="Result"/> is <see cref="InboxClaimResult.Claimed"/>.</summary>
    public InboxClaimToken? Token { get; init; }

    public static InboxClaimAttempt Claimed(InboxClaimToken token) => new()
    {
        Result = InboxClaimResult.Claimed,
        Token = token,
    };

    public static InboxClaimAttempt AlreadyProcessed() => new()
    {
        Result = InboxClaimResult.AlreadyProcessed,
    };

    public static InboxClaimAttempt ClaimHeldByOther() => new()
    {
        Result = InboxClaimResult.ClaimHeldByOther,
    };
}
