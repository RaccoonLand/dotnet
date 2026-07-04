using CleanArchitectureSample.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitectureSample.Infrastructure.Persistence.Queries.DependencyInjection;

public static class QueriesPersistenceExtensions
{
    public static IServiceCollection AddCleanArchitectureSampleQueriesPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("QueryConnection")
            ?? throw new InvalidOperationException("Connection string 'QueryConnection' is not configured.");

        services.AddDbContext<CleanArchitectureSampleQueryDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IQueryDbContext>(sp => sp.GetRequiredService<CleanArchitectureSampleQueryDbContext>());

        return services;
    }
}
