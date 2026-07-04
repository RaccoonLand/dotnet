using CleanArchitectureSample.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Modules.Persistence.SqlServer.Commands;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

namespace CleanArchitectureSample.Infrastructure.Persistence.Commands.DependencyInjection;

public static class CommandsPersistenceExtensions
{
    public static IServiceCollection AddCleanArchitectureSampleCommandsPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("CommandConnection")
            ?? throw new InvalidOperationException("Connection string 'CommandConnection' is not configured.");

        services.AddDbContext<CleanArchitectureSampleCommandDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
            options.AddRaccoonLandCommandInterceptors(
                new OutboxOptions { Table = "OutboxEvent" },
                sp.GetService<ICurrentExecutionContext>());
        });

        services.AddScoped<ICommandDbContext>(sp =>
            sp.GetRequiredService<CleanArchitectureSampleCommandDbContext>());

        return services;
    }
}
