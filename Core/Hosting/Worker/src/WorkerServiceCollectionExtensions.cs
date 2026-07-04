using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Core.ExecutionContext.Abstractions;

namespace RaccoonLand.Core.Hosting.Worker;

/// <summary>Registers worker hosting services for RaccoonLand.</summary>
public static class WorkerServiceCollectionExtensions
{
    /// <summary>
    /// Registers the worker dispatcher, execution context, and default response handler. Does not register
    /// <see cref="RequestProcessing.Abstractions.Dispatch.IRequestDispatcher"/> — call
    /// <c>AddRaccoonLandRequestProcessing</c> from <c>RaccoonLand.Core.RequestProcessing</c> in the same host.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="wireExecutionContext">
    /// When <c>true</c> (default), registers <see cref="ICurrentExecutionContext"/> as
    /// <see cref="WorkerExecutionContext"/> when no implementation exists yet. Worker-only hosts should keep
    /// this enabled. Combined API + worker hosts that already register an HTTP execution context should pass
    /// <c>false</c> and wire <see cref="ICurrentExecutionContext"/> themselves.
    /// </param>
    public static IServiceCollection AddRaccoonLandWorker(
        this IServiceCollection services,
        bool wireExecutionContext = true)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<WorkerExecutionContext>();
        services.TryAddSingleton<WorkerRequestDispatcher>();
        services.TryAddSingleton<IPipelineResponseHandler, DefaultPipelineResponseHandler>();

        if (wireExecutionContext)
        {
            services.TryAddScoped<ICurrentExecutionContext>(sp => sp.GetRequiredService<WorkerExecutionContext>());
        }

        return services;
    }
}
