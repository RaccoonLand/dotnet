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
///   "TenantIdHeaderMultiValueMode": "SingleValueOnly",
///   "CorrelationIdHeader": "X-Correlation-Id",
///   "CorrelationIdHeaderMultiValueMode": "SingleValueOnly",
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

    /// <summary>
    /// How to resolve <see cref="TenantIdHeader"/> when the request sends multiple values.
    /// Default: <see cref="MultiValueHeaderMode.SingleValueOnly"/>.
    /// </summary>
    public MultiValueHeaderMode TenantIdHeaderMultiValueMode { get; set; } = MultiValueHeaderMode.SingleValueOnly;

    /// <summary>Request header read for <see cref="RaccoonLand.Core.ExecutionContext.Abstractions.ICurrentExecutionContext.CorrelationId"/>.</summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// How to resolve <see cref="CorrelationIdHeader"/> when the request sends multiple values.
    /// Default: <see cref="MultiValueHeaderMode.SingleValueOnly"/>.
    /// </summary>
    public MultiValueHeaderMode CorrelationIdHeaderMultiValueMode { get; set; } = MultiValueHeaderMode.SingleValueOnly;

    /// <summary>
    /// When no correlation header or <see cref="System.Diagnostics.Activity"/> trace id is present, generate
    /// a new identifier. Defaults to <see langword="true"/> for tracing DX; set <see langword="false"/> to opt out.
    /// </summary>
    public bool GenerateCorrelationIdWhenMissing { get; set; } = true;

    /// <summary>
    /// When a correlation id is resolved or generated, write it to the response using
    /// <see cref="CorrelationIdHeader"/>. Defaults to <see langword="true"/>; this emits an external-facing
    /// response header — set <see langword="false"/> when API shape or privacy requires an explicit opt-in.
    /// </summary>
    public bool EchoCorrelationIdInResponse { get; set; } = true;
}
