using System.Security.Claims;
using RaccoonLand.Modules.Security.Authorization.Claims.Configuration;
using RaccoonLand.Modules.Security.Authorization.Claims.Tests.Support;

namespace RaccoonLand.Modules.Security.Authorization.Claims.Tests.Configuration;

public sealed class ClaimRequirementTests
{
    [Fact]
    public void IsSatisfiedBy_WhenNoAllowedValues_MatchesAnyClaimOfType()
    {
        var requirement = new ClaimRequirement("role", []);
        var principal = ClaimsTestHelpers.Authenticated(new Claim("role", "anything"));

        Assert.True(requirement.IsSatisfiedBy(principal));
    }

    [Fact]
    public void IsSatisfiedBy_WhenNoAllowedValuesAndClaimMissing_ReturnsFalse()
    {
        var requirement = new ClaimRequirement("role", []);
        var principal = ClaimsTestHelpers.Authenticated(new Claim("other", "x"));

        Assert.False(requirement.IsSatisfiedBy(principal));
    }

    [Fact]
    public void IsSatisfiedBy_WhenValueMatchesOneOfAllowed_ReturnsTrue()
    {
        var requirement = new ClaimRequirement("role", ["admin", "manager"]);
        var principal = ClaimsTestHelpers.Authenticated(new Claim("role", "manager"));

        Assert.True(requirement.IsSatisfiedBy(principal));
    }

    [Fact]
    public void IsSatisfiedBy_WhenValueNotInAllowed_ReturnsFalse()
    {
        var requirement = new ClaimRequirement("role", ["admin", "manager"]);
        var principal = ClaimsTestHelpers.Authenticated(new Claim("role", "user"));

        Assert.False(requirement.IsSatisfiedBy(principal));
    }
}
