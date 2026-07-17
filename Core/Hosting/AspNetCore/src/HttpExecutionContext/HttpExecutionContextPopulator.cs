using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

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

        return ResolveHeaderValue(httpContext, options.TenantIdHeader, options.TenantIdHeaderMultiValueMode);
    }

    private static string? ResolveCorrelationId(HttpContext httpContext, HttpExecutionContextOptions options)
    {
        var correlationId = ResolveHeaderValue(
            httpContext,
            options.CorrelationIdHeader,
            options.CorrelationIdHeaderMultiValueMode);
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

    private static string? ResolveHeaderValue(
        HttpContext httpContext,
        string? headerName,
        MultiValueHeaderMode multiValueMode)
    {
        if (string.IsNullOrWhiteSpace(headerName))
        {
            return null;
        }

        if (!httpContext.Request.Headers.TryGetValue(headerName, out var values))
        {
            return null;
        }

        return ApplyMultiValueMode(values, multiValueMode);
    }

    private static string? ApplyMultiValueMode(StringValues values, MultiValueHeaderMode mode)
    {
        var nonEmpty = values
            .Where(static v => !string.IsNullOrWhiteSpace(v))
            .Select(static v => v!.Trim())
            .ToArray();

        return mode switch
        {
            MultiValueHeaderMode.FirstValue => nonEmpty.Length > 0 ? nonEmpty[0] : null,
            MultiValueHeaderMode.SingleValueOnly => nonEmpty.Length == 1 ? nonEmpty[0] : null,
            MultiValueHeaderMode.Join => nonEmpty.Length > 0 ? string.Join(',', nonEmpty) : null,
            _ => nonEmpty.Length == 1 ? nonEmpty[0] : null,
        };
    }
}
