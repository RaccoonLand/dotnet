using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Configuration;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Data;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Support;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.DependencyInjection;

public sealed class MessageLocalizationSqlServerRegistrationTests
{
    [Fact]
    public void AddFromConfiguration_BindsOptionsAndRegistersCoreServices()
    {
        var configuration = TestConfiguration.FromDictionary(new Dictionary<string, string?>
        {
            ["MessageLocalization:ConnectionString"] = "Server=.;Database=Loc;",
            ["MessageLocalization:ServiceName"] = "Svc",
            ["MessageLocalization:ApplicationName"] = "App",
            ["MessageLocalization:DefaultCulture"] = "fa-IR",
        });

        var services = new ServiceCollection();
        services.AddRaccoonLandMessageLocalizationSqlServer(configuration);

        Assert.Contains(services, d => d.ServiceType == typeof(IMessageLocalization));
        Assert.Contains(services, d => d.ServiceType == typeof(MessageLocalizationStore));
        Assert.Contains(services, d => d.ServiceType == typeof(IMessageLocalizationRepository));
        Assert.Contains(services, d => d.ServiceType == typeof(IHostedService));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MessageLocalizationSqlServerOptions>>().Value;

        Assert.Equal("Server=.;Database=Loc;", options.ConnectionString);
        Assert.Equal("Svc", options.ServiceName);
        Assert.Equal("App", options.ApplicationName);
        Assert.Equal("fa-IR", options.DefaultCulture);
        Assert.IsType<SqlServerMessageLocalization>(provider.GetRequiredService<IMessageLocalization>());
    }

    [Fact]
    public void AddFromConfigureAction_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandMessageLocalizationSqlServer(o =>
        {
            o.ConnectionString = "cs";
            o.ServiceName = "svc";
            o.ApplicationName = "app";
            o.RefreshInterval = TimeSpan.FromSeconds(30);
        });

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MessageLocalizationSqlServerOptions>>().Value;

        Assert.Equal("cs", options.ConnectionString);
        Assert.Equal(TimeSpan.FromSeconds(30), options.RefreshInterval);
    }

    [Theory]
    [InlineData(null, "svc", "app")]
    [InlineData("", "svc", "app")]
    [InlineData("   ", "svc", "app")]
    [InlineData("cs", null, "app")]
    [InlineData("cs", "", "app")]
    [InlineData("cs", "svc", null)]
    [InlineData("cs", "svc", "  ")]
    public void Add_WhenRequiredValuesMissing_FailsValidation(
        string? connectionString,
        string? serviceName,
        string? applicationName)
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandMessageLocalizationSqlServer(o =>
        {
            o.ConnectionString = connectionString!;
            o.ServiceName = serviceName!;
            o.ApplicationName = applicationName!;
        });

        Assert.Throws<OptionsValidationException>(
            () => services.BuildServiceProvider()
                .GetRequiredService<IOptions<MessageLocalizationSqlServerOptions>>().Value);
    }

    [Fact]
    public void Add_DoesNotReplaceExistingCurrentCultureProvider()
    {
        var custom = new FixedCultureProvider(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentCultureProvider>(custom);
        services.AddRaccoonLandMessageLocalizationSqlServer(o =>
        {
            o.ConnectionString = "cs";
            o.ServiceName = "svc";
            o.ApplicationName = "app";
        });

        var resolved = services.BuildServiceProvider().GetRequiredService<ICurrentCultureProvider>();
        Assert.Same(custom, resolved);
    }

    [Fact]
    public void Add_RegistersValidatePredicate()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandMessageLocalizationSqlServer(o =>
        {
            o.ConnectionString = "cs";
            o.ServiceName = "svc";
            o.ApplicationName = "app";
        });

        Assert.Contains(
            services,
            d => d.ServiceType == typeof(IValidateOptions<MessageLocalizationSqlServerOptions>));
    }
}
