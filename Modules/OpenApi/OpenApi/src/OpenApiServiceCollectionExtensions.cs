using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using RaccoonLand.Modules.OpenApi.Abstractions;

namespace RaccoonLand.Modules.OpenApi;

/// <summary>
/// Registers configuration-driven OpenAPI document generation backed by <c>Microsoft.AspNetCore.OpenApi</c>.
/// </summary>
public static class OpenApiServiceCollectionExtensions
{
    private const string DocumentNameToken = "{documentName}";

    /// <summary>
    /// Binds <see cref="OpenApiDocumentOptions"/> from configuration and registers an OpenAPI document with
    /// document info and an optional JWT Bearer security scheme. Skips document registration when
    /// <see cref="OpenApiDocumentOptions.Enabled"/> is <see langword="false"/> (the finalized options snapshot
    /// is still registered for consumers that read <see cref="IOptions{TOptions}"/>).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when options are invalid (empty document/route/scheme names, or a route pattern that does not
    /// include the <c>{documentName}</c> token).
    /// </exception>
    public static IServiceCollection AddRaccoonLandOpenApi(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = OpenApiDocumentOptions.SectionName,
        Action<OpenApiDocumentOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var options = BuildOptions(configuration, sectionName, configureOptions);
        ValidateOptions(options);

        // Single finalized snapshot for DI and registration (avoids dual bind / double configureOptions).
        services.AddSingleton(Options.Create(options));

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

    private static OpenApiDocumentOptions BuildOptions(
        IConfiguration configuration,
        string sectionName,
        Action<OpenApiDocumentOptions>? configureOptions)
    {
        var section = configuration.GetSection(sectionName);
        var options = section.Get<OpenApiDocumentOptions>() ?? new OpenApiDocumentOptions();
        configureOptions?.Invoke(options);
        return options;
    }

    private static void ValidateOptions(OpenApiDocumentOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.DocumentName))
        {
            throw new InvalidOperationException(
                $"{nameof(OpenApiDocumentOptions.DocumentName)} cannot be null, empty, or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(options.RoutePattern))
        {
            throw new InvalidOperationException(
                $"{nameof(OpenApiDocumentOptions.RoutePattern)} cannot be null, empty, or whitespace.");
        }

        if (!options.RoutePattern.Contains(DocumentNameToken, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{nameof(OpenApiDocumentOptions.RoutePattern)} must contain the '{DocumentNameToken}' token " +
                $"so MapOpenApi and UI providers stay aligned with {nameof(OpenApiDocumentOptions.DocumentName)} " +
                $"(for example '/openapi/{DocumentNameToken}.json').");
        }

        if (options.Security is null)
        {
            throw new InvalidOperationException(
                $"{nameof(OpenApiDocumentOptions.Security)} cannot be null.");
        }

        if (options.Security.EnableJwtBearer && string.IsNullOrWhiteSpace(options.Security.SchemeName))
        {
            throw new InvalidOperationException(
                $"{nameof(OpenApiSecurityOptions.SchemeName)} cannot be null, empty, or whitespace when " +
                $"{nameof(OpenApiSecurityOptions.EnableJwtBearer)} is true.");
        }
    }
}
