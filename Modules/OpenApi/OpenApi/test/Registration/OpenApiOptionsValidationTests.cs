using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.OpenApi.Abstractions;
using RaccoonLand.Modules.OpenApi.Tests.Support;

namespace RaccoonLand.Modules.OpenApi.Tests.Registration;

public sealed class OpenApiOptionsValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddRaccoonLandOpenApi_WhenDocumentNameWhitespace_Throws(string documentName)
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandOpenApi(
                TestConfiguration.Empty(),
                configureOptions: o => o.DocumentName = documentName));

        Assert.Contains(nameof(OpenApiDocumentOptions.DocumentName), ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IOptions<OpenApiDocumentOptions>));
        Assert.False(OpenApiTestHelpers.HasAspNetCoreOpenApiRegistration(services));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddRaccoonLandOpenApi_WhenRoutePatternWhitespace_Throws(string routePattern)
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandOpenApi(
                TestConfiguration.Empty(),
                configureOptions: o => o.RoutePattern = routePattern));

        Assert.Contains(nameof(OpenApiDocumentOptions.RoutePattern), ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IOptions<OpenApiDocumentOptions>));
    }

    [Fact]
    public void AddRaccoonLandOpenApi_WhenRoutePatternMissingDocumentNameToken_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandOpenApi(
                TestConfiguration.Empty(),
                configureOptions: o => o.RoutePattern = "/openapi/v1.json"));

        Assert.Contains("{documentName}", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IOptions<OpenApiDocumentOptions>));
    }

    [Fact]
    public void AddRaccoonLandOpenApi_WhenSecurityNull_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandOpenApi(
                TestConfiguration.Empty(),
                configureOptions: o => o.Security = null!));

        Assert.Contains(nameof(OpenApiDocumentOptions.Security), ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IOptions<OpenApiDocumentOptions>));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddRaccoonLandOpenApi_WhenJwtEnabledAndSchemeNameWhitespace_Throws(string schemeName)
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandOpenApi(
                TestConfiguration.Empty(),
                configureOptions: o =>
                {
                    o.Security.EnableJwtBearer = true;
                    o.Security.SchemeName = schemeName;
                }));

        Assert.Contains(nameof(OpenApiSecurityOptions.SchemeName), ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IOptions<OpenApiDocumentOptions>));
    }

    [Fact]
    public void AddRaccoonLandOpenApi_WhenJwtDisabledAndSchemeNameWhitespace_Succeeds()
    {
        var services = OpenApiTestHelpers.CreateServices(configureOptions: o =>
        {
            o.Security.EnableJwtBearer = false;
            o.Security.SchemeName = " ";
        });

        var options = OpenApiTestHelpers.GetOptions(services);
        Assert.False(options.Security.EnableJwtBearer);
        Assert.Equal(" ", options.Security.SchemeName);
    }
}
