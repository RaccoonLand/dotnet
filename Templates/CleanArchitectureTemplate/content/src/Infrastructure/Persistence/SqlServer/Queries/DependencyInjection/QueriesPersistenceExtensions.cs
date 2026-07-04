using CleanArchitectureTemplate.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitectureTemplate.Infrastructure.Persistence.Queries.DependencyInjection;

public static class QueriesPersistenceExtensions
{
    public static IServiceCollection AddTemplateAppQueriesPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("QueryConnection")
            ?? throw new InvalidOperationException("Connection string 'QueryConnection' is not configured.");

        services.AddDbContext<TemplateQueryDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IQueryDbContext>(sp =>
            sp.GetRequiredService<TemplateQueryDbContext>());

        return services;
    }
}