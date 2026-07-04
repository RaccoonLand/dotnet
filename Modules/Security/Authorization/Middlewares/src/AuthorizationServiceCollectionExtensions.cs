using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

namespace RaccoonLand.Modules.Security.Authorization.Middlewares;

/// <summary>
/// Registration for the authorization pipeline middleware. Registers the middleware as a singleton
/// (stateless; the <see cref="RaccoonLand.Modules.Security.Authorization.Abstractions.IAuthorizationProvider"/>
/// is resolved per request from <see cref="PipelineContext.RequestServices"/>). Call this, then add the
/// middleware to a pipeline with <c>pipeline.UseMiddleware&lt;AuthorizationMiddleware&gt;()</c>.
/// <para>
/// An <c>IAuthorizationProvider</c> must be registered separately (for example by a provider package such as
/// the claim-based provider).
/// </para>
/// </summary>
public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddRaccoonLandAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<AuthorizationMiddleware>();

        return services;
    }
}
