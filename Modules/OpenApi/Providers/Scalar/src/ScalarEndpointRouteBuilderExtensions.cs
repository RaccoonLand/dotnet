using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.OpenApi.Abstractions;
using Scalar.AspNetCore;

namespace RaccoonLand.Modules.OpenApi.Scalar;

/// <summary>
/// Maps the Scalar API reference UI against the OpenAPI document served by <c>RaccoonLand.Modules.OpenApi</c>.
/// </summary>
public static class ScalarEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps Scalar pointing at the configured OpenAPI document route when
    /// <see cref="OpenApiDocumentOptions.Enabled"/> is <see langword="true"/>; otherwise a no-op.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configure">Optional Scalar customization (theme, layout, auth, and so on).</param>
    public static IEndpointRouteBuilder MapRaccoonLandScalar(
        this IEndpointRouteBuilder endpoints,
        Action<ScalarOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetService<IOptions<OpenApiDocumentOptions>>()?.Value;
        if (options is null || !options.Enabled)
        {
            return endpoints;
        }

        endpoints.MapScalarApiReference(scalar =>
        {
            scalar.WithOpenApiRoutePattern(options.RoutePattern)
                  .WithTitle(options.Title);
            configure?.Invoke(scalar);
        });

        return endpoints;
    }
}
