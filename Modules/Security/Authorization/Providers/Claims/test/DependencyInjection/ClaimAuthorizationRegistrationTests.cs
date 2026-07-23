using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Claims.Configuration;
using RaccoonLand.Modules.Security.Authorization.Claims.Principals;
using RaccoonLand.Modules.Security.Authorization.Claims.Provider;

namespace RaccoonLand.Modules.Security.Authorization.Claims.Tests.DependencyInjection;

public sealed class ClaimAuthorizationRegistrationTests
{
    [Fact]
    public void AddRaccoonLandClaimAuthorization_RegistersProviderAsScoped()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandClaimAuthorization();

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IAuthorizationProvider));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(ClaimAuthorizationProvider), descriptor.ImplementationType);
    }

    [Fact]
    public void AddRaccoonLandClaimAuthorization_RegistersDefaultClaimsPrincipalAccessor()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandClaimAuthorization();

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IClaimsPrincipalAccessor));
        Assert.Equal(typeof(HttpContextClaimsPrincipalAccessor), descriptor.ImplementationType);
    }

    [Fact]
    public void AddRaccoonLandClaimAuthorization_CodeOverload_AppliesOptions()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandClaimAuthorization(options =>
            options.AllowAnonymous("Anon.Request"));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ClaimAuthorizationOptions>>().Value;

        Assert.Contains("Anon.Request", options.AnonymousRequests);
    }

    [Fact]
    public void AddRaccoonLandClaimAuthorization_ConfigurationOverload_BindsAnonymousAndPolicies()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnonymousRequests:0"] = "Anon.Request",
                ["Policies:Secured.Request:Claims:0:ClaimType"] = "role",
                ["Policies:Secured.Request:Claims:0:AllowedValues:0"] = "admin",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddRaccoonLandClaimAuthorization(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ClaimAuthorizationOptions>>().Value;

        Assert.Contains("Anon.Request", options.AnonymousRequests);
        Assert.True(options.Policies.TryGetValue("Secured.Request", out var policy));
        var requirement = Assert.Single(policy!.Requirements);
        Assert.Equal("role", requirement.ClaimType);
        Assert.Equal(["admin"], requirement.AllowedValues);
    }

    [Fact]
    public void AddRaccoonLandClaimAuthorization_DoesNotOverwriteExistingProvider()
    {
        var services = new ServiceCollection();
        var custom = new StubProvider();
        services.AddSingleton<IAuthorizationProvider>(custom);

        services.AddRaccoonLandClaimAuthorization();

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IAuthorizationProvider));
        Assert.Same(custom, descriptor.ImplementationInstance);
    }

    private sealed class StubProvider : IAuthorizationProvider
    {
        public Task<AuthorizationDecision> AuthorizeAsync(
            AuthorizationContext context,
            CancellationToken cancellationToken)
            => Task.FromResult(AuthorizationDecision.Allow());
    }
}
