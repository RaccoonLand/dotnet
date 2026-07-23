using RaccoonLand.Modules.Security.Authorization.Abstractions;

namespace RaccoonLand.Modules.Security.Authorization.Middlewares.Tests.Support;

/// <summary>
/// Records the context and cancellation token passed to <see cref="AuthorizeAsync"/> and returns a fixed decision.
/// </summary>
internal sealed class FakeAuthorizationProvider(AuthorizationDecision decision) : IAuthorizationProvider
{
    public int CallCount { get; private set; }
    public AuthorizationContext? LastContext { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    public Task<AuthorizationDecision> AuthorizeAsync(
        AuthorizationContext context,
        CancellationToken cancellationToken)
    {
        CallCount++;
        LastContext = context;
        LastCancellationToken = cancellationToken;
        return Task.FromResult(decision);
    }
}
