using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.OpenApi.Abstractions;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace RaccoonLand.Modules.OpenApi.Swagger;

/// <summary>
/// Mounts Swagger UI against the OpenAPI document served by <c>RaccoonLand.Modules.OpenApi</c>.
/// </summary>
public static class SwaggerUiApplicationBuilderExtensions
{
    /// <summary>
    /// Enables Swagger UI pointing at the configured OpenAPI document route when
    /// <see cref="OpenApiDocumentOptions.Enabled"/> is <see langword="true"/>; otherwise a no-op.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configure">Optional Swagger UI customization (route prefix, theme, and so on).</param>
    public static IApplicationBuilder UseRaccoonLandSwaggerUI(
        this IApplicationBuilder app,
        Action<SwaggerUIOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.ApplicationServices.GetService<IOptions<OpenApiDocumentOptions>>()?.Value;
        if (options is null || !options.Enabled)
        {
            return app;
        }

        var documentUrl = options.RoutePattern.Replace("{documentName}", options.DocumentName, StringComparison.Ordinal);

        app.UseSwaggerUI(ui =>
        {
            ui.SwaggerEndpoint(documentUrl, options.Title);
            configure?.Invoke(ui);
        });

        return app;
    }
}
