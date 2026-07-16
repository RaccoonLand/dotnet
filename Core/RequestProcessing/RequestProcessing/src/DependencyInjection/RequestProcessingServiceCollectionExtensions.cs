using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Dispatch;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Dispatch;
using RaccoonLand.Core.RequestProcessing.Pipeline;

namespace RaccoonLand.Core.RequestProcessing.DependencyInjection;

/// <summary>
/// Registers the RaccoonLand request-processing pipeline: scans assemblies for endpoint implementations,
/// registers them in DI and the <see cref="EndpointInvokerRegistry"/>, and registers a singleton factory for
/// <see cref="CompiledPipelines"/>. The command/query pipelines are built when that singleton is first resolved.
/// </summary>
public static class RequestProcessingServiceCollectionExtensions
{
    /// <summary>
    /// Scans <paramref name="scanAssemblies"/> for <see cref="IEndpoint{TRequest}"/> and
    /// <see cref="IEndpoint{TRequest,TResponse}"/> implementations, registers them, and registers a
    /// <see cref="CompiledPipelines"/> factory. Pipeline composition (including optional middleware callbacks)
    /// runs when <see cref="CompiledPipelines"/> is first resolved from DI — not immediately when this method
    /// returns. When no assemblies are supplied, the calling assembly is scanned.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureCommandPipeline">Optional callback to add middleware to the command pipeline.</param>
    /// <param name="configureQueryPipeline">Optional callback to add middleware to the query pipeline.</param>
    /// <param name="scanAssemblies">Assemblies to scan for endpoint types.</param>
    public static IServiceCollection AddRaccoonLandRequestProcessing(
        this IServiceCollection services,
        Action<IPipelineBuilder>? configureCommandPipeline = null,
        Action<IPipelineBuilder>? configureQueryPipeline = null,
        params Assembly[] scanAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(scanAssemblies);

        if (scanAssemblies.Length == 0)
        {
            scanAssemblies = [Assembly.GetCallingAssembly()];
        }

        // Same assembly listed twice would re-register endpoints and trip duplicate-request checks.
        var assemblies = scanAssemblies.Distinct().ToArray();

        var registry = new EndpointInvokerRegistry();

        foreach (var assembly in assemblies)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            // Fail fast: do not catch ReflectionTypeLoadException — a partially loadable assembly is a
            // startup configuration problem, not something to silently skip.
            foreach (var type in assembly.GetTypes())
            {
                if (type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
                {
                    TryRegisterEndpoint(services, registry, type);
                }
            }
        }

        services.AddSingleton(registry);
        services.AddSingleton(sp =>
        {
            var commandBuilder = new CommandPipelineBuilder(sp, registry);
            configureCommandPipeline?.Invoke(commandBuilder);

            var queryBuilder = new QueryPipelineBuilder(sp, registry);
            configureQueryPipeline?.Invoke(queryBuilder);

            return new CompiledPipelines(
                commandBuilder.Build(),
                queryBuilder.Build());
        });
        services.AddSingleton<IRequestDispatcher, RequestDispatcher>();

        return services;
    }

    private static void TryRegisterEndpoint(
        IServiceCollection services,
        EndpointInvokerRegistry registry,
        Type endpointType)
    {
        var (requestType, responseType) = GetEndpointShape(endpointType);

        if (requestType is null || !typeof(IRequestBase).IsAssignableFrom(requestType))
        {
            return;
        }

        services.AddScoped(endpointType);
        var kind = ClassifyRequestKind(requestType);

        if (responseType is null)
        {
            registry.RegisterVoid(requestType, endpointType, kind);
        }
        else
        {
            registry.RegisterResponse(requestType, responseType, endpointType, kind);
        }
    }

    /// <summary>
    /// Returns the single <see cref="IEndpoint{TRequest}"/> / <see cref="IEndpoint{TRequest,TResponse}"/> shape
    /// on <paramref name="type"/>. Zero matches → (null, null). More than one match → throws.
    /// </summary>
    private static (Type? RequestType, Type? ResponseType) GetEndpointShape(Type type)
    {
        var shapes = type.GetInterfaces()
            .Where(i => i.IsGenericType)
            .Select(i =>
            {
                var definition = i.GetGenericTypeDefinition();
                var arguments = i.GetGenericArguments();

                if (definition == typeof(IEndpoint<,>))
                {
                    return (RequestType: (Type?)arguments[0], ResponseType: (Type?)arguments[1], Interface: i);
                }

                if (definition == typeof(IEndpoint<>))
                {
                    return (RequestType: (Type?)arguments[0], ResponseType: (Type?)null, Interface: i);
                }

                return (RequestType: null, ResponseType: null, Interface: i);
            })
            .Where(shape => shape.RequestType is not null)
            .ToArray();

        if (shapes.Length == 0)
        {
            return (null, null);
        }

        if (shapes.Length > 1)
        {
            var names = string.Join(", ", shapes.Select(s => s.Interface.ToString()));
            throw new InvalidOperationException(
                $"Endpoint type '{type.FullName}' implements multiple IEndpoint interfaces ({names}). " +
                "Each endpoint class must implement exactly one IEndpoint<TRequest> or IEndpoint<TRequest, TResponse>. " +
                "Split handlers into separate types.");
        }

        return (shapes[0].RequestType, shapes[0].ResponseType);
    }

    private static RequestKind ClassifyRequestKind(Type requestType)
        => Array.Exists(
            requestType.GetInterfaces(),
            i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>))
            ? RequestKind.Query
            : RequestKind.Command;
}
