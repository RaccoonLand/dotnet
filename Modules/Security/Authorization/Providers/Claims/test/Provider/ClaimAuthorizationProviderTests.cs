using System.Security.Claims;
using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Claims.Configuration;
using RaccoonLand.Modules.Security.Authorization.Claims.Tests.Support;

namespace RaccoonLand.Modules.Security.Authorization.Claims.Tests.Provider;

public sealed class ClaimAuthorizationProviderTests
{
    [Fact]
    public async Task AuthorizeAsync_WhenNullContext_Throws()
    {
        var provider = ClaimsTestHelpers.CreateProvider(new ClaimAuthorizationOptions(), principal: null);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => provider.AuthorizeAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task AuthorizeAsync_WhenAnonymousRequest_AllowsEvenWithoutPrincipal()
    {
        var options = new ClaimAuthorizationOptions();
        options.AllowAnonymous(ClaimsTestHelpers.RequestName);
        var provider = ClaimsTestHelpers.CreateProvider(options, principal: null);

        var decision = await Authorize(provider);

        Assert.Equal(AuthorizationStatus.Allowed, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenNoPrincipal_ReturnsUnauthenticated()
    {
        var provider = ClaimsTestHelpers.CreateProvider(new ClaimAuthorizationOptions(), principal: null);

        var decision = await Authorize(provider);

        Assert.Equal(AuthorizationStatus.Unauthenticated, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenPrincipalNotAuthenticated_ReturnsUnauthenticated()
    {
        var provider = ClaimsTestHelpers.CreateProvider(
            new ClaimAuthorizationOptions(),
            ClaimsTestHelpers.AnonymousIdentity());

        var decision = await Authorize(provider);

        Assert.Equal(AuthorizationStatus.Unauthenticated, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenAuthenticatedButNoPolicy_ReturnsDenied()
    {
        var provider = ClaimsTestHelpers.CreateProvider(
            new ClaimAuthorizationOptions(),
            ClaimsTestHelpers.Authenticated());

        var decision = await Authorize(provider);

        Assert.Equal(AuthorizationStatus.Denied, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenEmptyPolicyAndAuthenticated_ReturnsAllowed()
    {
        var options = new ClaimAuthorizationOptions();
        options.RequireAuthenticated(ClaimsTestHelpers.RequestName);
        var provider = ClaimsTestHelpers.CreateProvider(options, ClaimsTestHelpers.Authenticated());

        var decision = await Authorize(provider);

        Assert.Equal(AuthorizationStatus.Allowed, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenPolicySatisfied_ReturnsAllowed()
    {
        var options = new ClaimAuthorizationOptions();
        options.RequireClaim(ClaimsTestHelpers.RequestName, "role", "admin");
        var provider = ClaimsTestHelpers.CreateProvider(
            options,
            ClaimsTestHelpers.Authenticated(new Claim("role", "admin")));

        var decision = await Authorize(provider);

        Assert.Equal(AuthorizationStatus.Allowed, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenPolicyUnsatisfied_ReturnsDenied()
    {
        var options = new ClaimAuthorizationOptions();
        options.RequireClaim(ClaimsTestHelpers.RequestName, "role", "admin");
        var provider = ClaimsTestHelpers.CreateProvider(
            options,
            ClaimsTestHelpers.Authenticated(new Claim("role", "user")));

        var decision = await Authorize(provider);

        Assert.Equal(AuthorizationStatus.Denied, decision.Status);
    }

    private static Task<AuthorizationDecision> Authorize(Claims.Provider.ClaimAuthorizationProvider provider)
        => provider.AuthorizeAsync(new AuthorizationContext(ClaimsTestHelpers.RequestName), CancellationToken.None);
}
