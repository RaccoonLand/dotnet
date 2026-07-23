using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Configuration;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Data;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Provider;

namespace RaccoonLand.Modules.Security.Authorization.SqlServer.Tests.DependencyInjection;

public sealed class SqlAuthorizationRegistrationTests
{
    [Fact]
    public void AddRaccoonLandSqlServerAuthorization_RegistersProviderScopedAndRepositorySingleton()
    {
        var services = new ServiceCollection();

        services.AddRaccoonLandSqlServerAuthorization(ValidConfigure());

        var providerDescriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IAuthorizationProvider));
        Assert.Equal(ServiceLifetime.Scoped, providerDescriptor.Lifetime);
        Assert.Equal(typeof(SqlAuthorizationProvider), providerDescriptor.ImplementationType);

        var repositoryDescriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(ISqlAuthorizationRepository));
        Assert.Equal(ServiceLifetime.Singleton, repositoryDescriptor.Lifetime);
        Assert.Equal(typeof(SqlAuthorizationRepository), repositoryDescriptor.ImplementationType);
    }

    [Fact]
    public void AddRaccoonLandSqlServerAuthorization_WithValidCodeOptions_ResolvesOptions()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandSqlServerAuthorization(ValidConfigure());

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SqlAuthorizationOptions>>().Value;

        Assert.Equal("Server=.;Database=Security;Trusted_Connection=True", options.ConnectionString);
    }

    [Fact]
    public void AddRaccoonLandSqlServerAuthorization_WhenConnectionStringMissing_FailsValidation()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandSqlServerAuthorization(options =>
        {
            options.AnonymousRequestsProcedure = "dbo.Anon";
            options.AllowedRequestsProcedure = "dbo.Allowed";
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SqlAuthorizationOptions>>();

        Assert.Throws<OptionsValidationException>(() => _ = options.Value);
    }

    [Fact]
    public void AddRaccoonLandSqlServerAuthorization_WhenProcedureNamesMissing_FailsValidation()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandSqlServerAuthorization(options =>
            options.ConnectionString = "Server=.;Database=Security;Trusted_Connection=True");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SqlAuthorizationOptions>>();

        Assert.Throws<OptionsValidationException>(() => _ = options.Value);
    }

    [Fact]
    public void AddRaccoonLandSqlServerAuthorization_ConfigurationOverload_BindsOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authorization:SqlServer:ConnectionString"] = "Server=.;Database=Security;Trusted_Connection=True",
                ["Authorization:SqlServer:AnonymousRequestsProcedure"] = "dbo.GetAnonymousRequests",
                ["Authorization:SqlServer:AllowedRequestsProcedure"] = "dbo.GetAllowedRequestsForUser",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddRaccoonLandSqlServerAuthorization(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SqlAuthorizationOptions>>().Value;

        Assert.Equal("dbo.GetAnonymousRequests", options.AnonymousRequestsProcedure);
        Assert.Equal("dbo.GetAllowedRequestsForUser", options.AllowedRequestsProcedure);
    }

    [Fact]
    public void AddRaccoonLandSqlServerAuthorization_DoesNotOverwriteExistingProvider()
    {
        var services = new ServiceCollection();
        var custom = new StubProvider();
        services.AddSingleton<IAuthorizationProvider>(custom);

        services.AddRaccoonLandSqlServerAuthorization(ValidConfigure());

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IAuthorizationProvider));
        Assert.Same(custom, descriptor.ImplementationInstance);
    }

    private static Action<SqlAuthorizationOptions> ValidConfigure()
        => options =>
        {
            options.ConnectionString = "Server=.;Database=Security;Trusted_Connection=True";
            options.AnonymousRequestsProcedure = "dbo.GetAnonymousRequests";
            options.AllowedRequestsProcedure = "dbo.GetAllowedRequestsForUser";
        };

    private sealed class StubProvider : IAuthorizationProvider
    {
        public Task<AuthorizationDecision> AuthorizeAsync(
            AuthorizationContext context,
            CancellationToken cancellationToken)
            => Task.FromResult(AuthorizationDecision.Allow());
    }
}
