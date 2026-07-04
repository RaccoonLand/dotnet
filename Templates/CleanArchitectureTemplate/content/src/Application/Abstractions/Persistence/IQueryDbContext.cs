using Microsoft.EntityFrameworkCore;

namespace CleanArchitectureTemplate.Application.Abstractions.Persistence;

public interface IQueryDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
}