using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Interceptors;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands;

/// <summary>
/// Resets in-memory outbox flush marks for an execution-strategy attempt. Complements
/// <c>TransactionRolledBack</c> so retry still works when the provider does not raise rollback callbacks
/// after a failed <c>CommitAsync</c>.
/// </summary>
internal static class OutboxAttemptCleanup
{
    public static void ResetForRetry(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        OutboxSaveChangesInterceptor.ResetAttemptState(context);
        context.GetService<OutboxWriter>()?.RestoreFlushedOnRollback();
    }
}
