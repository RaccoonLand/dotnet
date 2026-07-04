using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

/// <summary>Whether the current request flows through the command or the query pipeline.</summary>
public enum RequestKind
{
    Command,
    Query,
}

/// <summary>
/// The per-request context that flows through a pipeline — the CQRS counterpart of ASP.NET Core's
/// <c>HttpContext</c>, but with no dependency on ASP.NET Core. It carries the typed request and its
/// eventual response, the scoped service provider, a cancellation token tied to the request lifetime,
/// and an <see cref="Items"/> bag for passing data between middleware.
/// <para>
/// Because it is host-agnostic, the same pipeline and middleware run unchanged in an API, a worker or a
/// test. A host adapter (for example an ASP.NET Core endpoint) is responsible for building this context
/// from its own request — supplying the request scope's <see cref="IServiceProvider"/> and cancellation
/// token.
/// </para>
/// </summary>
public sealed class PipelineContext
{
    /// <summary>Creates a context for a single request flowing through a pipeline.</summary>
    /// <param name="request">The command or query object being processed.</param>
    /// <param name="kind">Whether this is a command or a query.</param>
    /// <param name="requestServices">The scoped service provider for this request.</param>
    /// <param name="cancellationToken">A token tied to the request lifetime.</param>
    public PipelineContext(
        IRequestBase request,
        RequestKind kind,
        IServiceProvider requestServices,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestServices);

        Request = request;
        Kind = kind;
        RequestServices = requestServices;
        CancellationToken = cancellationToken;
    }

    /// <summary>The command or query object being processed.</summary>
    public IRequestBase Request { get; }

    /// <summary>Whether the current request flows through the command or the query pipeline.</summary>
    public RequestKind Kind { get; }

    /// <summary>
    /// The response envelope for this request. The terminal handler sets it by wrapping its result; middleware
    /// may set it to short-circuit the pipeline (for example a cache hit or a validation failure). Null until
    /// a handler or middleware produces one.
    /// </summary>
    public PipelineResponse? Response { get; set; }

    /// <summary>The scoped service provider for this request.</summary>
    public IServiceProvider RequestServices { get; }

    /// <summary>Cancellation token tied to the request lifetime.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>A per-request bag for sharing state across middleware.</summary>
    public IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
}
