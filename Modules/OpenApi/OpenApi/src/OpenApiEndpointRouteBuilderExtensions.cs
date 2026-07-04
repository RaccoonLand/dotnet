using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.OpenApi.Abstractions;

namespace RaccoonLand.Modules.OpenApi;

/// <summary>
/// Maps the OpenAPI document endpoint that serves the generated JSON consumed by the UI providers.
/// </summary>
public static class OpenApiEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the OpenAPI document at <see cref="OpenApiDocumentOptions.RoutePattern"/> when
    /// <see cref="OpenApiDocumentOptions.Enabled"/> is <see langword="true"/>; otherwise a no-op.
    /// </summary>
    public static IEndpointRouteBuilder MapRaccoonLandOpenApi(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetService<IOptions<OpenApiDocumentOptions>>()?.Value;
        if (options is null || !options.Enabled)
        {
            return endpoints;
        }

        endpoints.MapOpenApi(options.RoutePattern);
        return endpoints;
    }
}
