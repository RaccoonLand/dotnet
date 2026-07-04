using Microsoft.AspNetCore.Builder;

namespace RaccoonLand.Core.Hosting.AspNetCore.HttpExceptionHandling;

/// <summary>HTTP pipeline registration for <see cref="HttpExceptionHandlingMiddleware"/>.</summary>
public static class HttpExceptionHandlingApplicationBuilderExtensions
{
    /// <summary>
    /// Catches unhandled exceptions from downstream HTTP middleware and MVC endpoints. Place early in the
    /// pipeline (for example immediately after <c>UseHttpsRedirection</c>) so controller and pipeline failures
    /// are shaped consistently.
    /// </summary>
    public static IApplicationBuilder UseRaccoonLandHttpExceptionHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<HttpExceptionHandlingMiddleware>();
    }
}
