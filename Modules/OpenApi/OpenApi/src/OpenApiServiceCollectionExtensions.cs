using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using RaccoonLand.Modules.OpenApi.Abstractions;

namespace RaccoonLand.Modules.OpenApi;

/// <summary>
/// Registers configuration-driven OpenAPI document generation backed by <c>Microsoft.AspNetCore.OpenApi</c>.
/// </summary>
public static class OpenApiServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="OpenApiDocumentOptions"/> from configuration and registers an OpenAPI document with
    /// document info and an optional JWT Bearer security scheme. Skips registration when
    /// <see cref="OpenApiDocumentOptions.Enabled"/> is <see langword="false"/>.
    /// </summary>
    public static IServiceCollection AddRaccoonLandOpenApi(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = OpenApiDocumentOptions.SectionName,
        Action<OpenApiDocumentOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionName);
        services.Configure<OpenApiDocumentOptions>(section);

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        var options = section.Get<OpenApiDocumentOptions>() ?? new OpenApiDocumentOptions();
        configureOptions?.Invoke(options);

        if (!options.Enabled)
        {
            return services;
        }

        services.AddOpenApi(options.DocumentName, openApiOptions =>
        {
            openApiOptions.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info ??= new OpenApiInfo();
                document.Info.Title = options.Title;
                document.Info.Version = options.Version;
                document.Info.Description = options.Description;
                return Task.CompletedTask;
            });

            if (options.Security.EnableJwtBearer)
            {
                openApiOptions.AddDocumentTransformer(
                    new BearerSecuritySchemeDocumentTransformer(options.Security));
            }
        });

        return services;
    }
}
