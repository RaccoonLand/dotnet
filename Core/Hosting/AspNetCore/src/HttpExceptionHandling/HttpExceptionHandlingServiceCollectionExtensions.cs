using Microsoft.Extensions.DependencyInjection;

namespace RaccoonLand.Core.Hosting.AspNetCore.HttpExceptionHandling;

/// <summary>DI registration for <see cref="HttpExceptionHandlingMiddleware"/>.</summary>
public static class HttpExceptionHandlingServiceCollectionExtensions
{
    /// <summary>Registers <see cref="HttpExceptionHandlingOptions"/> for the HTTP exception-handling middleware.</summary>
    public static IServiceCollection AddRaccoonLandHttpExceptionHandling(
        this IServiceCollection services,
        Action<HttpExceptionHandlingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<HttpExceptionHandlingOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
