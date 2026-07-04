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
/// registers them in DI and the <see cref="EndpointInvokerRegistry"/>, and builds the command/query pipelines.
/// </summary>
public static class RequestProcessingServiceCollectionExtensions
{
    /// <summary>
    /// Scans <paramref name="scanAssemblies"/> for <see cref="IEndpoint{TRequest}"/> and
    /// <see cref="IEndpoint{TRequest,TResponse}"/> implementations, registers them, and builds the command and
    /// query pipelines. When no assemblies are supplied, the calling assembly is scanned.
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

        if (scanAssemblies.Length == 0)
        {
            scanAssemblies = [Assembly.GetCallingAssembly()];
        }

        var registry = new EndpointInvokerRegistry();

        foreach (var assembly in scanAssemblies)
        {
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

            return new CompiledPipelines
            {
                Command = commandBuilder.Build(),
                Query = queryBuilder.Build(),
            };
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

    private static (Type? RequestType, Type? ResponseType) GetEndpointShape(Type type)
    {
        foreach (var @interface in type.GetInterfaces().Where(i => i.IsGenericType))
        {
            var definition = @interface.GetGenericTypeDefinition();
            var arguments = @interface.GetGenericArguments();

            if (definition == typeof(IEndpoint<,>))
            {
                return (arguments[0], arguments[1]);
            }

            if (definition == typeof(IEndpoint<>))
            {
                return (arguments[0], null);
            }
        }

        return (null, null);
    }

    private static RequestKind ClassifyRequestKind(Type requestType)
        => Array.Exists(
            requestType.GetInterfaces(),
            i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>))
            ? RequestKind.Query
            : RequestKind.Command;
}
