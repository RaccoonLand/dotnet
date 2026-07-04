using Microsoft.EntityFrameworkCore;

namespace CleanArchitectureSample.Application.Abstractions.Persistence;

public interface ICommandDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
