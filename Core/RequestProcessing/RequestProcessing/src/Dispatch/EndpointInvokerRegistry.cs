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
    private sealed class Entry(PipelineDelegate invoker, RequestMetadata metadata)
    {
        public PipelineDelegate Invoker { get; } = invoker;
        public RequestMetadata Metadata { get; } = metadata;
    }

    private readonly Dictionary<Type, Entry> _entries = [];

    public void RegisterResponse(Type requestType, Type responseType, Type endpointType, RequestKind kind)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(responseType);
        ArgumentNullException.ThrowIfNull(endpointType);

        EnsureImplementsEndpointShape(
            endpointType,
            typeof(IEndpoint<,>).MakeGenericType(requestType, responseType),
            $"IEndpoint<{FormatType(requestType)}, {FormatType(responseType)}>");

        EnsureNotAlreadyRegistered(requestType);

        var invoker = (PipelineDelegate)typeof(EndpointInvokerRegistry)
            .GetMethod(nameof(BuildResponseInvoker), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(requestType, responseType)
            .Invoke(null, [endpointType])!;

        var metadata = new RequestMetadata(requestType, responseType, kind);
        _entries[requestType] = new Entry(invoker, metadata);
    }

    public void RegisterVoid(Type requestType, Type endpointType, RequestKind kind)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(endpointType);

        EnsureImplementsEndpointShape(
            endpointType,
            typeof(IEndpoint<>).MakeGenericType(requestType),
            $"IEndpoint<{FormatType(requestType)}>");

        EnsureNotAlreadyRegistered(requestType);

        var invoker = (PipelineDelegate)typeof(EndpointInvokerRegistry)
            .GetMethod(nameof(BuildVoidInvoker), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(requestType)
            .Invoke(null, [endpointType])!;

        var metadata = new RequestMetadata(requestType, responseType: null, kind);
        _entries[requestType] = new Entry(invoker, metadata);
    }

    public PipelineDelegate Resolve(Type requestType)
        => TryGetEntry(requestType, out var entry)
            ? entry.Invoker
            : throw new InvalidOperationException(
                $"No endpoint is registered for request type '{requestType.FullName}'.");

    /// <summary>
    /// Returns the pre-built <see cref="RequestMetadata"/> captured at scan time, so the dispatcher and any
    /// consumer can obtain the request's structural facts (type, response type, kind) with a single
    /// dictionary lookup and zero runtime reflection. Callers that only need the kind read
    /// <c>ResolveMetadata(t).Kind</c>.
    /// </summary>
    public RequestMetadata ResolveMetadata(Type requestType)
        => TryGetEntry(requestType, out var entry)
            ? entry.Metadata
            : throw new InvalidOperationException(
                $"No endpoint is registered for request type '{requestType.FullName}'.");

    private bool TryGetEntry(Type requestType, out Entry entry)
        => _entries.TryGetValue(requestType, out entry!);

    private static void EnsureImplementsEndpointShape(
        Type endpointType,
        Type expectedInterface,
        string expectedInterfaceDisplayName)
    {
        if (expectedInterface.IsAssignableFrom(endpointType))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Endpoint type '{endpointType.FullName}' does not implement {expectedInterfaceDisplayName}. " +
            "Register the concrete endpoint type that matches the request/response shape, " +
            "or fix the RegisterResponse/RegisterVoid type arguments.");
    }

    private void EnsureNotAlreadyRegistered(Type requestType)
    {
        if (!_entries.ContainsKey(requestType))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Request type '{requestType.FullName}' is already registered. " +
            "Each request type may only be registered once (typically during startup).");
    }

    private static string FormatType(Type type)
        => type.Name;

    private static PipelineDelegate BuildResponseInvoker<TRequest, TResponse>(Type endpointType)
        where TRequest : IRequestBase
        => async context =>
        {
            var resolved = context.RequestServices.GetRequiredService(endpointType);
            if (resolved is not IEndpoint<TRequest, TResponse> endpoint)
            {
                throw new InvalidOperationException(
                    $"Resolved service '{endpointType.FullName}' is '{resolved.GetType().FullName}', " +
                    $"which is not assignable to IEndpoint<{typeof(TRequest).Name}, {typeof(TResponse).Name}>. " +
                    "Check DI registration for this endpoint.");
            }

            if (context.Request is not TRequest request)
            {
                throw new InvalidOperationException(
                    $"Pipeline request type mismatch: expected '{typeof(TRequest).FullName}' " +
                    $"but got '{context.Request.GetType().FullName}'.");
            }

            var result = await endpoint.ExecuteAsync(request, context.CancellationToken);
            context.Response = result.ToPipelineResponse();
        };

    private static PipelineDelegate BuildVoidInvoker<TRequest>(Type endpointType)
        where TRequest : IRequestBase
        => async context =>
        {
            var resolved = context.RequestServices.GetRequiredService(endpointType);
            if (resolved is not IEndpoint<TRequest> endpoint)
            {
                throw new InvalidOperationException(
                    $"Resolved service '{endpointType.FullName}' is '{resolved.GetType().FullName}', " +
                    $"which is not assignable to IEndpoint<{typeof(TRequest).Name}>. " +
                    "Check DI registration for this endpoint.");
            }

            if (context.Request is not TRequest request)
            {
                throw new InvalidOperationException(
                    $"Pipeline request type mismatch: expected '{typeof(TRequest).FullName}' " +
                    $"but got '{context.Request.GetType().FullName}'.");
            }

            var result = await endpoint.ExecuteAsync(request, context.CancellationToken);
            context.Response = result.ToPipelineResponse();
        };
}
