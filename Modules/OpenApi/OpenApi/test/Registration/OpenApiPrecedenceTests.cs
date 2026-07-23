using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.OpenApi.Abstractions;
using RaccoonLand.Modules.OpenApi.Tests.Support;

namespace RaccoonLand.Modules.OpenApi.Tests.Registration;

public sealed class OpenApiPrecedenceTests
{
    [Fact]
    public void AddRaccoonLandOpenApi_ConfigureOptionsOverridesConfiguration()
    {
        var configuration = TestConfiguration.FromDictionary(new Dictionary<string, string?>
        {
            ["OpenApi:Enabled"] = "true",
            ["OpenApi:DocumentName"] = "from-config",
            ["OpenApi:RoutePattern"] = "/openapi/{documentName}.json",
            ["OpenApi:Security:EnableJwtBearer"] = "false",
            ["OpenApi:Security:SchemeName"] = "ConfigScheme",
            ["OpenApi:Security:ApplyGlobally"] = "true",
        });

        var services = OpenApiTestHelpers.CreateServices(configuration, o =>
        {
            o.Enabled = false;
            o.DocumentName = "from-code";
            o.RoutePattern = "/docs/{documentName}.yaml";
            o.Security.EnableJwtBearer = true;
            o.Security.SchemeName = "CodeScheme";
            o.Security.ApplyGlobally = false;
        });

        var options = OpenApiTestHelpers.GetOptions(services);

        Assert.False(options.Enabled);
        Assert.Equal("from-code", options.DocumentName);
        Assert.Equal("/docs/{documentName}.yaml", options.RoutePattern);
        Assert.True(options.Security.EnableJwtBearer);
        Assert.Equal("CodeScheme", options.Security.SchemeName);
        Assert.False(options.Security.ApplyGlobally);
    }

    [Fact]
    public void AddRaccoonLandOpenApi_ConfigureOptionsInvokedExactlyOnce()
    {
        var invocations = 0;

        OpenApiTestHelpers.CreateServices(configureOptions: _ => invocations++);

        Assert.Equal(1, invocations);
    }

    [Fact]
    public void AddRaccoonLandOpenApi_ValidatesFinalOptionsAfterOverride()
    {
        var configuration = TestConfiguration.FromDictionary(new Dictionary<string, string?>
        {
            ["OpenApi:DocumentName"] = " ",
            ["OpenApi:RoutePattern"] = "/openapi/{documentName}.json",
        });

        // Invalid DocumentName from config is repaired by configureOptions before validation.
        var services = OpenApiTestHelpers.CreateServices(configuration, o => o.DocumentName = "repaired");
        Assert.Equal("repaired", OpenApiTestHelpers.GetOptions(services).DocumentName);

        // Valid config becomes invalid after override — validation must see the final value.
        var broken = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            broken.AddRaccoonLandOpenApi(
                TestConfiguration.FromDictionary(new Dictionary<string, string?>
                {
                    ["OpenApi:DocumentName"] = "v1",
                    ["OpenApi:RoutePattern"] = "/openapi/{documentName}.json",
                }),
                configureOptions: o => o.RoutePattern = "/openapi/v1.json"));
    }
}
