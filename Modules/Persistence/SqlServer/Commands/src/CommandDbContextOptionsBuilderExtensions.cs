using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Interceptors;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands;

/// <summary>
/// Registers the command-side interceptors (audit/concurrency and the transactional outbox) on a
/// <see cref="DbContextOptionsBuilder"/>.
/// </summary>
public static class CommandDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Adds the audit/concurrency and outbox SaveChanges interceptors.
    /// </summary>
    /// <param name="builder">The DbContext options builder.</param>
    /// <param name="outboxOptions">Where domain/service events are written.</param>
    /// <param name="currentExecutionContext">
    /// The execution context used by the audit interceptor to resolve the acting user. When <c>null</c>
    /// (no implementation wired up), <see cref="NullCurrentExecutionContext.Instance"/> is used as a fallback,
    /// so audit stamping still works and simply records a <c>null</c> user.
    /// </param>
    public static DbContextOptionsBuilder AddRaccoonLandCommandInterceptors(
        this DbContextOptionsBuilder builder,
        OutboxOptions outboxOptions,
        ICurrentExecutionContext? currentExecutionContext = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(outboxOptions);

        var executionContext = currentExecutionContext ?? NullCurrentExecutionContext.Instance;

        var interceptors = new IInterceptor[]
        {
            new AuditSaveChangesInterceptor(executionContext),
            new OutboxSaveChangesInterceptor(outboxOptions),
        };

        return builder.AddInterceptors(interceptors);
    }

    /// <summary>
    /// Adds the outbox-writer interceptor that drains the request-scoped
    /// <see cref="RaccoonLand.Modules.Persistence.Outbox.Abstraction.IOutboxWriter"/> buffer on
    /// <c>SaveChanges</c>. Requires the outbox
    /// services to be registered with <c>AddRaccoonLandOutbox&lt;TOutbox&gt;()</c>. Because the interceptor is
    /// request-scoped, this must be called from a scoped <paramref name="serviceProvider"/> (for example the
    /// one passed to <c>AddDbContext((sp, options) =&gt; ...)</c>).
    /// </summary>
    public static DbContextOptionsBuilder AddRaccoonLandOutboxInterceptor(
        this DbContextOptionsBuilder builder,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return builder.AddInterceptors(
            serviceProvider.GetRequiredService<OutboxWriterSaveChangesInterceptor>());
    }
}
