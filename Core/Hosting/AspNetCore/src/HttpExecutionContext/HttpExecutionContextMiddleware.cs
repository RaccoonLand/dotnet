using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;

/// <summary>
/// Populates <see cref="HttpExecutionContext"/> once per HTTP request. Place after
/// <c>UseAuthentication()</c> so claims are available on <see cref="HttpContext.User"/>.
/// </summary>
public sealed class HttpExecutionContextMiddleware
{
    private readonly RequestDelegate _next;

    public HttpExecutionContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext httpContext, IOptions<HttpExecutionContextOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);

        var executionContext = httpContext.RequestServices.GetRequiredService<HttpExecutionContext>();
        var resolved = HttpExecutionContextPopulator.Resolve(httpContext, options.Value);
        executionContext.Populate(resolved.UserId, resolved.TenantId, resolved.CorrelationId);
        HttpExecutionContextPopulator.EchoCorrelationId(httpContext, options.Value, resolved.CorrelationId);

        await _next(httpContext);
    }
}
