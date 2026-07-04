using Microsoft.EntityFrameworkCore;

namespace CleanArchitectureSample.Application.Abstractions.Persistence;

public interface IQueryDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
}
