using System.Security.Claims;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authorization.Claims.Configuration;
using RaccoonLand.Modules.Security.Authorization.Claims.Principals;
using RaccoonLand.Modules.Security.Authorization.Claims.Provider;

namespace RaccoonLand.Modules.Security.Authorization.Claims.Tests.Support;

internal sealed class FakeClaimsPrincipalAccessor(ClaimsPrincipal? principal) : IClaimsPrincipalAccessor
{
    public ClaimsPrincipal? Principal { get; } = principal;
}

internal static class ClaimsTestHelpers
{
    public const string RequestName = "Sample.Namespace.SampleRequest";

    public static ClaimsPrincipal Authenticated(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "TestAuth"));

    /// <summary>An identity with no authentication type — <c>IsAuthenticated</c> is false.</summary>
    public static ClaimsPrincipal AnonymousIdentity()
        => new(new ClaimsIdentity());

    public static ClaimAuthorizationProvider CreateProvider(
        ClaimAuthorizationOptions options,
        ClaimsPrincipal? principal)
        => new(new FakeClaimsPrincipalAccessor(principal), Options.Create(options));
}
