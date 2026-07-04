using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.ExecutionContext.Abstractions;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Interceptors;

/// <summary>
/// Fills the audit information of entities and refreshes the concurrency token of aggregates on save.
/// The acting user is taken from <see cref="ICurrentExecutionContext"/>. When no execution context is
/// available (for example a host that has not implemented it, or a background save with no scope), the
/// audit user falls back to <c>null</c> — see <see cref="GetCurrentUser"/>.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentExecutionContext _executionContext;

    /// <summary>
    /// Creates the interceptor with the execution context used to resolve the acting user. When
    /// <paramref name="executionContext"/> is <c>null</c>, <see cref="NullCurrentExecutionContext"/> is used
    /// as a fallback so the interceptor still works where no context is wired up.
    /// </summary>
    public AuditSaveChangesInterceptor(ICurrentExecutionContext? executionContext = null)
        => _executionContext = executionContext ?? NullCurrentExecutionContext.Instance;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// The user that stamps the audit fields. Returns the current <see cref="ICurrentExecutionContext.UserId"/>
    /// when the context is available; otherwise <c>null</c>. Override to customize.
    /// </summary>
    protected virtual string? GetCurrentUser()
        => _executionContext.IsAvailable ? _executionContext.UserId : null;

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var user = GetCurrentUser();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.SetCreatedAudit(now, user);
                        break;
                    case EntityState.Modified:
                        auditable.SetModifiedAudit(now, user);
                        break;
                }
            }

            // A modified aggregate gets a fresh concurrency token so concurrent edits are detected.
            if (entry.State == EntityState.Modified && entry.Entity is IAggregateRoot aggregate)
            {
                aggregate.RegenerateConcurrencyToken();
            }
        }
    }
}
