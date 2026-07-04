using Microsoft.AspNetCore.Builder;

namespace RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;

/// <summary>HTTP pipeline registration for <see cref="HttpExecutionContextMiddleware"/>.</summary>
public static class HttpExecutionContextApplicationBuilderExtensions
{
    /// <summary>
    /// Populates <see cref="RaccoonLand.Core.ExecutionContext.Abstractions.ICurrentExecutionContext"/> from the current HTTP request on every call.
    /// Place after <c>UseAuthentication()</c>.
    /// </summary>
    public static IApplicationBuilder UseRaccoonLandHttpExecutionContext(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<HttpExecutionContextMiddleware>();
    }
}
