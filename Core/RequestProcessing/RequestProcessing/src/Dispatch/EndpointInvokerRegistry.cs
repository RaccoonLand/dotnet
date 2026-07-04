using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace RaccoonLand.Core.RequestProcessing.Dispatch;

/// <summary>
/// Maps a request type to the terminal delegate that resolves its endpoint and invokes it, and records the
/// request's <see cref="RequestKind"/> so the dispatcher can pick the right pipeline without run-time
/// reflection.
/// </summary>
public sealed class EndpointInvokerRegistry
{
    private sealed class Entry(PipelineDelegate invoker, RequestKind kind)
    {
        public PipelineDelegate Invoker { get; } = invoker;
        public RequestKind Kind { get; } = kind;
    }

    private readonly Dictionary<Type, Entry> _entries = [];

    public void RegisterResponse(Type requestType, Type responseType, Type endpointType, RequestKind kind)
    {
        var invoker = (PipelineDelegate)typeof(EndpointInvokerRegistry)
            .GetMethod(nameof(BuildResponseInvoker), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(requestType, responseType)
            .Invoke(null, [endpointType])!;

        _entries[requestType] = new Entry(invoker, kind);
    }

    public void RegisterVoid(Type requestType, Type endpointType, RequestKind kind)
    {
        var invoker = (PipelineDelegate)typeof(EndpointInvokerRegistry)
            .GetMethod(nameof(BuildVoidInvoker), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(requestType)
            .Invoke(null, [endpointType])!;

        _entries[requestType] = new Entry(invoker, kind);
    }

    public PipelineDelegate Resolve(Type requestType)
        => TryGetEntry(requestType, out var entry)
            ? entry.Invoker
            : throw new InvalidOperationException(
                $"No endpoint is registered for request type '{requestType.FullName}'.");

    public RequestKind ResolveKind(Type requestType)
        => TryGetEntry(requestType, out var entry)
            ? entry.Kind
            : throw new InvalidOperationException(
                $"No endpoint is registered for request type '{requestType.FullName}'.");

    private bool TryGetEntry(Type requestType, out Entry entry)
        => _entries.TryGetValue(requestType, out entry!);

    private static PipelineDelegate BuildResponseInvoker<TRequest, TResponse>(Type endpointType)
        where TRequest : IRequestBase
        => async context =>
        {
            var endpoint = (IEndpoint<TRequest, TResponse>)context.RequestServices.GetRequiredService(endpointType);
            var result = await endpoint.ExecuteAsync((TRequest)context.Request, context.CancellationToken);
            context.Response = result.ToPipelineResponse();
        };

    private static PipelineDelegate BuildVoidInvoker<TRequest>(Type endpointType)
        where TRequest : IRequestBase
        => async context =>
        {
            var endpoint = (IEndpoint<TRequest>)context.RequestServices.GetRequiredService(endpointType);
            var result = await endpoint.ExecuteAsync((TRequest)context.Request, context.CancellationToken);
            context.Response = result.ToPipelineResponse();
        };
}
