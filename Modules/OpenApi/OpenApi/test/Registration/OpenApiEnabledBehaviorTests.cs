using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.OpenApi.Abstractions;
using RaccoonLand.Modules.OpenApi.Tests.Support;

namespace RaccoonLand.Modules.OpenApi.Tests.Registration;

public sealed class OpenApiEnabledBehaviorTests
{
    [Fact]
    public void AddRaccoonLandOpenApi_WhenEnabled_RegistersDocumentAndOptions()
    {
        var services = OpenApiTestHelpers.CreateServices(configureOptions: o => o.Enabled = true);

        Assert.NotNull(OpenApiTestHelpers.GetOptions(services));
        Assert.True(OpenApiTestHelpers.HasAspNetCoreOpenApiRegistration(services));
    }

    [Fact]
    public void AddRaccoonLandOpenApi_WhenDisabled_RegistersOptionsButSkipsDocument()
    {
        var services = OpenApiTestHelpers.CreateServices(configureOptions: o => o.Enabled = false);

        var options = OpenApiTestHelpers.GetOptions(services);
        Assert.False(options.Enabled);
        Assert.False(OpenApiTestHelpers.HasAspNetCoreOpenApiRegistration(services));
    }

    [Fact]
    public void MapRaccoonLandOpenApi_WhenEnabled_AddsEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRaccoonLandOpenApi(
            TestConfiguration.Empty(),
            configureOptions: o =>
            {
                o.Enabled = true;
                o.DocumentName = "v1";
                o.RoutePattern = "/openapi/{documentName}.json";
            });

        using var app = builder.Build();
        var before = OpenApiTestHelpers.CountEndpoints(app);

        app.MapRaccoonLandOpenApi();

        Assert.True(OpenApiTestHelpers.CountEndpoints(app) > before);
    }

    [Fact]
    public void MapRaccoonLandOpenApi_WhenDisabled_IsNoOp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRaccoonLandOpenApi(
            TestConfiguration.Empty(),
            configureOptions: o => o.Enabled = false);

        using var app = builder.Build();
        var before = OpenApiTestHelpers.CountEndpoints(app);

        app.MapRaccoonLandOpenApi();

        Assert.Equal(before, OpenApiTestHelpers.CountEndpoints(app));
        Assert.False(app.Services.GetRequiredService<IOptions<OpenApiDocumentOptions>>().Value.Enabled);
    }
}
