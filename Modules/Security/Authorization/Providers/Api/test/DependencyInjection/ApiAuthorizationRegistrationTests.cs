using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Api.Configuration;
using RaccoonLand.Modules.Security.Authorization.Api.Http;
using RaccoonLand.Modules.Security.Authorization.Api.Provider;

namespace RaccoonLand.Modules.Security.Authorization.Api.Tests.DependencyInjection;

public sealed class ApiAuthorizationRegistrationTests
{
    [Fact]
    public void AddRaccoonLandApiAuthorization_RegistersProviderAsScopedAndTypedClient()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandApiAuthorization(ValidConfigure());

        var providerDescriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IAuthorizationProvider));
        Assert.Equal(ServiceLifetime.Scoped, providerDescriptor.Lifetime);
        Assert.Equal(typeof(ApiAuthorizationProvider), providerDescriptor.ImplementationType);

        Assert.Contains(services, d => d.ServiceType == typeof(AuthorizationApiClient));
    }

    [Fact]
    public void AddRaccoonLandApiAuthorization_WithValidCodeOptions_ResolvesOptions()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandApiAuthorization(ValidConfigure());

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiAuthorizationOptions>>().Value;

        Assert.Equal(new Uri("https://policy.internal/api/"), options.BaseAddress);
    }

    [Fact]
    public void AddRaccoonLandApiAuthorization_WhenBaseAddressMissing_FailsValidationOnResolve()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandApiAuthorization(_ => { });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiAuthorizationOptions>>();

        Assert.Throws<OptionsValidationException>(() => _ = options.Value);
    }

    [Fact]
    public void AddRaccoonLandApiAuthorization_WhenAllowedPathMissingUserIdPlaceholder_FailsValidation()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandApiAuthorization(options =>
        {
            options.BaseAddress = new Uri("https://policy.internal/api/");
            options.AllowedRequestsPath = "users/allowed-requests";
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiAuthorizationOptions>>();

        Assert.Throws<OptionsValidationException>(() => _ = options.Value);
    }

    [Fact]
    public void AddRaccoonLandApiAuthorization_ConfigurationOverload_BindsOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authorization:Api:BaseAddress"] = "https://policy.internal/api/",
                ["Authorization:Api:EnableCache"] = "false",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddRaccoonLandApiAuthorization(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiAuthorizationOptions>>().Value;

        Assert.Equal(new Uri("https://policy.internal/api/"), options.BaseAddress);
    }

    [Fact]
    public void AddRaccoonLandApiAuthorization_DoesNotOverwriteExistingProvider()
    {
        var services = new ServiceCollection();
        var custom = new StubProvider();
        services.AddSingleton<IAuthorizationProvider>(custom);

        services.AddRaccoonLandApiAuthorization(ValidConfigure());

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IAuthorizationProvider));
        Assert.Same(custom, descriptor.ImplementationInstance);
    }

    private static Action<ApiAuthorizationOptions> ValidConfigure()
        => options => options.BaseAddress = new Uri("https://policy.internal/api/");

    private sealed class StubProvider : IAuthorizationProvider
    {
        public Task<AuthorizationDecision> AuthorizeAsync(
            AuthorizationContext context,
            CancellationToken cancellationToken)
            => Task.FromResult(AuthorizationDecision.Allow());
    }
}
