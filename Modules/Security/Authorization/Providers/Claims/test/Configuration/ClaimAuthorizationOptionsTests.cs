using RaccoonLand.Modules.Security.Authorization.Claims.Configuration;

namespace RaccoonLand.Modules.Security.Authorization.Claims.Tests.Configuration;

public sealed class ClaimAuthorizationOptionsTests
{
    private const string Request = "Some.Request";

    [Fact]
    public void AllowAnonymous_AddsRequestToAnonymousSet()
    {
        var options = new ClaimAuthorizationOptions();

        options.AllowAnonymous(Request);

        Assert.Contains(Request, options.AnonymousRequests);
        Assert.DoesNotContain(Request, options.Policies.Keys);
    }

    [Fact]
    public void RequireAuthenticated_CreatesEmptyPolicy()
    {
        var options = new ClaimAuthorizationOptions();

        options.RequireAuthenticated(Request);

        Assert.True(options.Policies.TryGetValue(Request, out var policy));
        Assert.Empty(policy!.Requirements);
        Assert.Empty(policy.Assertions);
    }

    [Fact]
    public void RequireClaim_AddsRequirementWithTypeAndValues()
    {
        var options = new ClaimAuthorizationOptions();

        options.RequireClaim(Request, "role", "admin", "manager");

        var requirement = Assert.Single(options.Policies[Request].Requirements);
        Assert.Equal("role", requirement.ClaimType);
        Assert.Equal(["admin", "manager"], requirement.AllowedValues);
    }

    [Fact]
    public void RequireClaim_WithNoValues_LeavesAllowedValuesEmpty()
    {
        var options = new ClaimAuthorizationOptions();

        options.RequireClaim(Request, "role");

        var requirement = Assert.Single(options.Policies[Request].Requirements);
        Assert.Empty(requirement.AllowedValues);
    }

    [Fact]
    public void RequireAssertion_AddsAssertionToPolicy()
    {
        var options = new ClaimAuthorizationOptions();

        options.RequireAssertion(Request, _ => true);

        Assert.Single(options.Policies[Request].Assertions);
    }

    [Fact]
    public void MultipleRequirementsForSameRequest_AccumulateInSinglePolicy()
    {
        var options = new ClaimAuthorizationOptions();

        options
            .RequireClaim(Request, "role", "admin")
            .RequireClaim(Request, "scope", "read")
            .RequireAssertion(Request, _ => true);

        var policy = options.Policies[Request];
        Assert.Equal(2, policy.Requirements.Count);
        Assert.Single(policy.Assertions);
        Assert.Single(options.Policies);
    }
}
