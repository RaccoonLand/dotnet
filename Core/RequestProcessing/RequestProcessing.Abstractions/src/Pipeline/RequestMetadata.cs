using System.Collections.Concurrent;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

/// <summary>
/// Immutable structural facts about a request type — its CLR <see cref="RequestType"/>, the optional
/// <see cref="ResponseType"/> from <see cref="IRequest{TResponse}"/> (null for void <see cref="IRequest"/>),
/// and the pipeline <see cref="Kind"/>. Discovered once at startup by the dispatcher's endpoint scan and
/// attached to every <see cref="PipelineContext"/> so middleware never has to walk interfaces at request
/// time.
/// <para>
/// Production code always receives this from <c>PipelineContext.Metadata</c>. The static
/// <see cref="For(Type, RequestKind)"/> helper is for test authors and late-bound scenarios that build a
/// <see cref="PipelineContext"/> by hand; it memoizes results in a process-wide cache so repeated lookups
/// are allocation-free.
/// </para>
/// </summary>
public sealed record RequestMetadata
{
    private static readonly ConcurrentDictionary<(Type Request, RequestKind Kind), RequestMetadata> Cache = new();

    /// <summary>
    /// Constructs a new metadata instance. Prefer <see cref="For(Type, RequestKind)"/> from test code so
    /// repeated resolutions of the same request type share a cached instance.
    /// </summary>
    /// <param name="requestType">The concrete CLR type of the request.</param>
    /// <param name="responseType">The <c>TResponse</c> from <see cref="IRequest{TResponse}"/>, or null for a void <see cref="IRequest"/>.</param>
    /// <param name="kind">Whether this request flows through the command or the query pipeline.</param>
    public RequestMetadata(Type requestType, Type? responseType, RequestKind kind)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        RequestType = requestType;
        ResponseType = responseType;
        Kind = kind;
    }

    /// <summary>The concrete CLR type of the request being processed.</summary>
    public Type RequestType { get; }

    /// <summary>
    /// The response payload type from <see cref="IRequest{TResponse}"/>. <see langword="null"/> when the
    /// request is a void <see cref="IRequest"/> (no meaningful <c>Result</c> to observe).
    /// </summary>
    public Type? ResponseType { get; }

    /// <summary>Whether this request flows through the command or the query pipeline.</summary>
    public RequestKind Kind { get; }

    /// <summary>
    /// Returns a memoized <see cref="RequestMetadata"/> for the given request type and pipeline kind. The
    /// <c>TResponse</c> is discovered by walking <paramref name="requestType"/>'s interfaces looking for
    /// <see cref="IRequest{TResponse}"/>; results are cached in a process-wide <see cref="ConcurrentDictionary{TKey,TValue}"/>
    /// so subsequent calls for the same <c>(Type, Kind)</c> pair are O(1) and allocation-free.
    /// </summary>
    /// <param name="requestType">The concrete request CLR type.</param>
    /// <param name="kind">Whether this request flows through the command or the query pipeline.</param>
    public static RequestMetadata For(Type requestType, RequestKind kind)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        return Cache.GetOrAdd((requestType, kind), static key => Build(key.Request, key.Kind));
    }

    private static RequestMetadata Build(Type requestType, RequestKind kind)
    {
        Type? responseType = null;
        foreach (var @interface in requestType.GetInterfaces())
        {
            if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IRequest<>))
            {
                responseType = @interface.GetGenericArguments()[0];
                break;
            }
        }

        return new RequestMetadata(requestType, responseType, kind);
    }
}
