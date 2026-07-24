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
    /// <summary>
    /// Creates a context for a single request flowing through a pipeline. The <paramref name="metadata"/>
    /// is normally supplied by the dispatcher from its pre-built endpoint registry; tests and late-bound
    /// callers may build one on the fly with <see cref="RequestMetadata.For(Type, RequestKind)"/>.
    /// </summary>
    /// <param name="request">The command or query object being processed.</param>
    /// <param name="requestServices">The scoped service provider for this request.</param>
    /// <param name="metadata">Immutable structural facts about the request (type, response type, kind).</param>
    /// <param name="cancellationToken">A token tied to the request lifetime.</param>
    public PipelineContext(
        IRequestBase request,
        IServiceProvider requestServices,
        RequestMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestServices);
        ArgumentNullException.ThrowIfNull(metadata);

        if (!metadata.RequestType.IsInstanceOfType(request))
        {
            throw new ArgumentException(
                $"Metadata is for '{metadata.RequestType.FullName}' but the request instance is " +
                $"'{request.GetType().FullName}'. Metadata must describe the actual request type.",
                nameof(metadata));
        }

        Request = request;
        RequestServices = requestServices;
        Metadata = metadata;
        CancellationToken = cancellationToken;
    }

    /// <summary>The command or query object being processed.</summary>
    public IRequestBase Request { get; }

    /// <summary>
    /// Immutable structural facts about the current request: its CLR type, the optional response type from
    /// <see cref="Cqrs.IRequest{TResponse}"/>, and the pipeline <see cref="RequestKind"/>. Middleware should
    /// read this instead of re-inspecting <see cref="Request"/>'s interfaces at request time.
    /// </summary>
    public RequestMetadata Metadata { get; }

    /// <summary>
    /// Whether the current request flows through the command or the query pipeline. Delegates to
    /// <see cref="Metadata"/>.
    /// </summary>
    public RequestKind Kind => Metadata.Kind;

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
