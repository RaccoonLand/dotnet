using System.Security.Claims;
using RaccoonLand.Modules.Security.Authorization.Claims.Configuration;
using RaccoonLand.Modules.Security.Authorization.Claims.Tests.Support;

namespace RaccoonLand.Modules.Security.Authorization.Claims.Tests.Configuration;

public sealed class ClaimAuthorizationPolicyTests
{
    [Fact]
    public void IsSatisfiedBy_WhenEmptyPolicy_ReturnsTrue()
    {
        var policy = new ClaimAuthorizationPolicy();
        var principal = ClaimsTestHelpers.Authenticated();

        Assert.True(policy.IsSatisfiedBy(principal));
    }

    [Fact]
    public void IsSatisfiedBy_WhenAllRequirementsAndAssertionsHold_ReturnsTrue()
    {
        var policy = new ClaimAuthorizationPolicy();
        policy.Requirements.Add(new ClaimRequirement("role", ["admin"]));
        policy.Requirements.Add(new ClaimRequirement("scope", ["read"]));
        policy.Assertions.Add(p => p.HasClaim("tenant", "acme"));

        var principal = ClaimsTestHelpers.Authenticated(
            new Claim("role", "admin"),
            new Claim("scope", "read"),
            new Claim("tenant", "acme"));

        Assert.True(policy.IsSatisfiedBy(principal));
    }

    [Fact]
    public void IsSatisfiedBy_WhenOneRequirementFails_ReturnsFalse()
    {
        var policy = new ClaimAuthorizationPolicy();
        policy.Requirements.Add(new ClaimRequirement("role", ["admin"]));
        policy.Requirements.Add(new ClaimRequirement("scope", ["read"]));

        var principal = ClaimsTestHelpers.Authenticated(new Claim("role", "admin"));

        Assert.False(policy.IsSatisfiedBy(principal));
    }

    [Fact]
    public void IsSatisfiedBy_WhenOneAssertionFails_ReturnsFalse()
    {
        var policy = new ClaimAuthorizationPolicy();
        policy.Assertions.Add(_ => true);
        policy.Assertions.Add(_ => false);

        var principal = ClaimsTestHelpers.Authenticated();

        Assert.False(policy.IsSatisfiedBy(principal));
    }
}
