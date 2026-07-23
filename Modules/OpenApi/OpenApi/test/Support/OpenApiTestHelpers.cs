using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.OpenApi.Abstractions;

namespace RaccoonLand.Modules.OpenApi.Tests.Support;

internal static class OpenApiTestHelpers
{
    public static IServiceCollection CreateServices(
        IConfiguration? configuration = null,
        Action<OpenApiDocumentOptions>? configureOptions = null,
        string sectionName = OpenApiDocumentOptions.SectionName)
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandOpenApi(
            configuration ?? TestConfiguration.Empty(),
            sectionName,
            configureOptions);
        return services;
    }

    public static OpenApiDocumentOptions GetOptions(IServiceCollection services)
        => services.BuildServiceProvider().GetRequiredService<IOptions<OpenApiDocumentOptions>>().Value;

    /// <summary>
    /// <see cref="Microsoft.AspNetCore.OpenApi.OpenApiServiceCollectionExtensions.AddOpenApi"/> registers
    /// configuration for <see cref="OpenApiOptions"/>; our snapshot alone does not.
    /// </summary>
    public static bool HasAspNetCoreOpenApiRegistration(IServiceCollection services)
        => services.Any(static d =>
            d.ServiceType == typeof(IConfigureOptions<OpenApiOptions>)
            || d.ServiceType == typeof(IConfigureNamedOptions<OpenApiOptions>)
            || d.ServiceType == typeof(OpenApiOptions));

    public static int CountEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints.DataSources.Sum(ds => ds.Endpoints.Count);
}
