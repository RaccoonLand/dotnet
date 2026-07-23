using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Interceptors;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

internal static class PersistenceTestHelpers
{
    /// <summary>Builds an InMemory audit context wired with the audit interceptor and a unique store.</summary>
    public static AuditTestDbContext CreateAuditContext(ICurrentExecutionContext? executionContext)
    {
        var options = new DbContextOptionsBuilder<AuditTestDbContext>()
            .UseInMemoryDatabase($"audit-{Guid.NewGuid():N}")
            .AddInterceptors(new AuditSaveChangesInterceptor(executionContext))
            .Options;

        return new AuditTestDbContext(options);
    }

    /// <summary>Builds a <see cref="TestCommandDbContext"/> over an InMemory store (no interceptors needed).</summary>
    public static TestCommandDbContext CreateCommandContext()
    {
        var options = new DbContextOptionsBuilder<TestCommandDbContext>()
            .UseInMemoryDatabase($"command-{Guid.NewGuid():N}")
            .Options;

        return new TestCommandDbContext(options);
    }
}
