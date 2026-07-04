using CapabilityCentricSample.Shared.Persistence.Commands;
using CapabilityCentricSample.Shared.Persistence.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Modules.Persistence.SqlServer.Commands;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

namespace CapabilityCentricSample.Shared.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddCapabilityCentricSamplePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var commandConnectionString = configuration.GetConnectionString("CommandConnection")
            ?? throw new InvalidOperationException("Connection string 'CommandConnection' is not configured.");

        services.AddDbContext<CapabilityCentricSampleCommandDbContext>((sp, options) =>
        {
            options.UseSqlServer(commandConnectionString);
            options.AddRaccoonLandCommandInterceptors(
                new OutboxOptions { Table = "OutboxEvent" },
                sp.GetService<ICurrentExecutionContext>());
        });

        var queryConnectionString = configuration.GetConnectionString("QueryConnection")
            ?? throw new InvalidOperationException("Connection string 'QueryConnection' is not configured.");

        services.AddDbContext<CapabilityCentricSampleQueryDbContext>(options =>
            options.UseSqlServer(queryConnectionString));

        return services;
    }
}
