using Microsoft.EntityFrameworkCore;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Interceptors;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Interceptors.OutboxWriter;

public sealed class OutboxWriterSaveChangesInterceptorTests
{
    private static OutboxChannelRegistry CreateRegistryWithTestChannel()
    {
        var registry = new OutboxChannelRegistry();
        registry.Register<ITestOutbox>(new OutboxChannelOptions { Table = "TestOutbox" });
        return registry;
    }

    [Fact]
    public async Task SavedChangesAsync_WhenPendingMessagesAndNoAmbientTransaction_Throws()
    {
        // Same guard as the event outbox interceptor: without an ambient transaction the outbox INSERT would
        // auto-commit and break atomicity, so we fail loudly on first save.
        var registry = CreateRegistryWithTestChannel();
        var writer = new Commands.Outbox.OutboxWriter(registry);
        writer.Enqueue<ITestOutbox>(new SamplePayload { Value = "x" });
        var interceptor = new OutboxWriterSaveChangesInterceptor(registry, writer);

        var options = new DbContextOptionsBuilder<AuditTestDbContext>()
            .UseInMemoryDatabase($"writer-no-tx-{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;
        await using var context = new AuditTestDbContext(options);
        context.PlainRecords.Add(new PlainRecord { Name = "trigger-save" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SavedChangesAsync_WhenBufferIsEmpty_DoesNotThrowEvenWithoutTransaction()
    {
        // Empty buffer short-circuits before checking the ambient transaction so a save with nothing enqueued
        // never fails on a plain DbContext.
        var registry = CreateRegistryWithTestChannel();
        var writer = new Commands.Outbox.OutboxWriter(registry);
        var interceptor = new OutboxWriterSaveChangesInterceptor(registry, writer);

        var options = new DbContextOptionsBuilder<AuditTestDbContext>()
            .UseInMemoryDatabase($"writer-empty-{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;
        await using var context = new AuditTestDbContext(options);
        context.PlainRecords.Add(new PlainRecord { Name = "trigger-save" });

        var affected = await context.SaveChangesAsync();

        Assert.Equal(1, affected);
    }
}
