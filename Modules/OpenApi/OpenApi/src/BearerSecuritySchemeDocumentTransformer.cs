using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using RaccoonLand.Modules.OpenApi.Abstractions;

namespace RaccoonLand.Modules.OpenApi;

/// <summary>
/// Adds a JWT Bearer security scheme to the OpenAPI document (and, when configured, a global security
/// requirement) so a UI such as Swagger UI or Scalar shows an Authorize dialog.
/// </summary>
internal sealed class BearerSecuritySchemeDocumentTransformer(OpenApiSecurityOptions security)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(security.SchemeName))
        {
            throw new InvalidOperationException(
                $"{nameof(OpenApiSecurityOptions.SchemeName)} cannot be null, empty, or whitespace.");
        }

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = security.BearerFormat,
            In = ParameterLocation.Header,
            Description = security.Description,
        };

        document.Components ??= new OpenApiComponents();

        if (document.Components.SecuritySchemes is not null
            && document.Components.SecuritySchemes.ContainsKey(security.SchemeName))
        {
            throw new InvalidOperationException(
                $"OpenAPI security scheme component '{security.SchemeName}' is already registered. " +
                $"Choose a different {nameof(OpenApiSecurityOptions.SchemeName)} or remove the existing component.");
        }

        document.AddComponent(security.SchemeName, scheme);

        if (security.ApplyGlobally)
        {
            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(security.SchemeName, document)] = [],
            });
        }

        return Task.CompletedTask;
    }
}
