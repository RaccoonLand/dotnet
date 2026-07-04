namespace RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;

/// <summary>
/// Settings for resolving <see cref="RaccoonLand.Core.ExecutionContext.Abstractions.ICurrentExecutionContext"/> from the current HTTP request.
/// </summary>
/// <example>
/// appsettings.json:
/// <code>
/// "HttpExecutionContext": {
///   "UserIdClaim": "sub",
///   "TenantIdClaim": "tenant_id",
///   "TenantIdHeader": "X-Tenant-Id",
///   "CorrelationIdHeader": "X-Correlation-Id",
///   "GenerateCorrelationIdWhenMissing": true,
///   "EchoCorrelationIdInResponse": true
/// }
/// </code>
/// </example>
public sealed class HttpExecutionContextOptions
{
    /// <summary>Default root configuration section name (<c>HttpExecutionContext</c>).</summary>
    public const string SectionName = "HttpExecutionContext";

    /// <summary>Claim type used for <see cref="RaccoonLand.Core.ExecutionContext.Abstractions.ICurrentExecutionContext.UserId"/>.</summary>
    public string UserIdClaim { get; set; } = "sub";

    /// <summary>Optional claim type used for <see cref="RaccoonLand.Core.ExecutionContext.Abstractions.ICurrentExecutionContext.TenantId"/>.</summary>
    public string? TenantIdClaim { get; set; }

    /// <summary>
    /// Optional request header used for <see cref="RaccoonLand.Core.ExecutionContext.Abstractions.ICurrentExecutionContext.TenantId"/> when the
    /// claim is absent.
    /// </summary>
    public string? TenantIdHeader { get; set; }

    /// <summary>Request header read for <see cref="RaccoonLand.Core.ExecutionContext.Abstractions.ICurrentExecutionContext.CorrelationId"/>.</summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// When no correlation header or <see cref="System.Diagnostics.Activity"/> trace id is present, generate
    /// a new identifier.
    /// </summary>
    public bool GenerateCorrelationIdWhenMissing { get; set; } = true;

    /// <summary>
    /// When a correlation id is resolved or generated, write it to the response using
    /// <see cref="CorrelationIdHeader"/>.
    /// </summary>
    public bool EchoCorrelationIdInResponse { get; set; } = true;
}
