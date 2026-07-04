using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;

internal static class HttpExecutionContextPopulator
{
    public static (string? UserId, string? TenantId, string? CorrelationId) Resolve(
        HttpContext httpContext,
        HttpExecutionContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);

        var principal = httpContext.User;
        var userId = ResolveClaimValue(principal, options.UserIdClaim);
        var tenantId = ResolveTenantId(httpContext, principal, options);
        var correlationId = ResolveCorrelationId(httpContext, options);

        return (userId, tenantId, correlationId);
    }

    public static void EchoCorrelationId(
        HttpContext httpContext,
        HttpExecutionContextOptions options,
        string? correlationId)
    {
        if (!options.EchoCorrelationIdInResponse
            || string.IsNullOrWhiteSpace(correlationId)
            || string.IsNullOrWhiteSpace(options.CorrelationIdHeader))
        {
            return;
        }

        if (!httpContext.Response.Headers.ContainsKey(options.CorrelationIdHeader))
        {
            httpContext.Response.Headers[options.CorrelationIdHeader] = correlationId;
        }
    }

    private static string? ResolveTenantId(
        HttpContext httpContext,
        ClaimsPrincipal principal,
        HttpExecutionContextOptions options)
    {
        var tenantId = ResolveClaimValue(principal, options.TenantIdClaim);
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            return tenantId;
        }

        return ResolveHeaderValue(httpContext, options.TenantIdHeader);
    }

    private static string? ResolveCorrelationId(HttpContext httpContext, HttpExecutionContextOptions options)
    {
        var correlationId = ResolveHeaderValue(httpContext, options.CorrelationIdHeader);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        var activityTraceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(activityTraceId) && activityTraceId != "00000000000000000000000000000000")
        {
            return activityTraceId;
        }

        if (options.GenerateCorrelationIdWhenMissing)
        {
            return Guid.CreateVersion7().ToString();
        }

        return null;
    }

    private static string? ResolveClaimValue(ClaimsPrincipal principal, string? claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType))
        {
            return null;
        }

        return principal.FindFirst(claimType)?.Value;
    }

    private static string? ResolveHeaderValue(HttpContext httpContext, string? headerName)
    {
        if (string.IsNullOrWhiteSpace(headerName))
        {
            return null;
        }

        return httpContext.Request.Headers.TryGetValue(headerName, out var values)
            ? values.ToString()
            : null;
    }
}
