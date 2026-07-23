using RaccoonLand.Modules.Persistence.Outbox.Abstraction;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Outbox.Writer;

public sealed class OutboxWriterTests
{
    private static OutboxChannelRegistry CreateRegistry()
    {
        var registry = new OutboxChannelRegistry();
        registry.Register<ITestOutbox>(new OutboxChannelOptions { Table = "TestOutbox" });
        registry.Register<IOtherOutbox>(new OutboxChannelOptions { Table = "OtherOutbox" });
        return registry;
    }

    private static OutboxWriter CreateWriter(RaccoonLand.Core.ExecutionContext.Abstractions.ICurrentExecutionContext? ctx = null)
        => new(CreateRegistry(), ctx);

    [Fact]
    public void Enqueue_WhenPayloadNull_ThrowsArgumentNullException()
    {
        var writer = CreateWriter();

        Assert.Throws<ArgumentNullException>(() => writer.Enqueue<ITestOutbox>(null!));
    }

    [Fact]
    public void Enqueue_WhenChannelNotRegistered_ThrowsInvalidOperationException()
    {
        var writer = CreateWriter();

        Assert.Throws<InvalidOperationException>(
            () => writer.Enqueue<IUnregisteredOutbox>(new SamplePayload()));
    }

    [Fact]
    public void Enqueue_WhenValid_KeepsMessagePending()
    {
        var writer = CreateWriter();

        writer.Enqueue<ITestOutbox>(new SamplePayload { Value = "x" });

        var batches = writer.GetPendingBatches();
        var batch = Assert.Single(batches);
        Assert.Equal(typeof(ITestOutbox), batch.ChannelType);
        Assert.Single(batch.Messages);
    }

    [Fact]
    public void Enqueue_WhenExplicitEventType_OverridesClrNameFallback()
    {
        var writer = CreateWriter();

        writer.Enqueue<ITestOutbox>(
            new SamplePayload(),
            new OutboxEnqueueOptions { EventType = "order.placed.v1" });

        var message = writer.GetPendingBatches().Single().Messages.Single();
        Assert.Equal("order.placed.v1", message.EventType);
    }

    [Fact]
    public void Enqueue_WhenNoEventType_FallsBackToPayloadClrName()
    {
        var writer = CreateWriter();

        writer.Enqueue<ITestOutbox>(new SamplePayload());

        var message = writer.GetPendingBatches().Single().Messages.Single();
        Assert.Equal(nameof(SamplePayload), message.EventType);
    }

    [Fact]
    public void Enqueue_WhenExecutionContextAvailable_StampsCreatedBy()
    {
        var writer = CreateWriter(new FakeExecutionContext { IsAvailable = true, UserId = "user-7" });

        writer.Enqueue<ITestOutbox>(new SamplePayload());

        var message = writer.GetPendingBatches().Single().Messages.Single();
        Assert.Equal("user-7", message.CreatedBy);
    }

    [Fact]
    public void Enqueue_WhenNoExecutionContext_LeavesCreatedByNull()
    {
        var writer = CreateWriter();

        writer.Enqueue<ITestOutbox>(new SamplePayload());

        var message = writer.GetPendingBatches().Single().Messages.Single();
        Assert.Null(message.CreatedBy);
    }

    [Fact]
    public void GetPendingBatches_WhenMultipleChannels_GroupsPerChannel()
    {
        var writer = CreateWriter();
        writer.Enqueue<ITestOutbox>(new SamplePayload { Value = "a" });
        writer.Enqueue<ITestOutbox>(new SamplePayload { Value = "b" });
        writer.Enqueue<IOtherOutbox>(new SamplePayload { Value = "c" });

        var batches = writer.GetPendingBatches();

        Assert.Equal(2, batches.Count);
        Assert.Equal(2, batches.Single(b => b.ChannelType == typeof(ITestOutbox)).Messages.Count);
        Assert.Single(batches.Single(b => b.ChannelType == typeof(IOtherOutbox)).Messages);
    }

    [Fact]
    public void MarkFlushed_MovesMessagesOutOfPending()
    {
        var writer = CreateWriter();
        writer.Enqueue<ITestOutbox>(new SamplePayload { Value = "a" });
        writer.Enqueue<ITestOutbox>(new SamplePayload { Value = "b" });
        var ids = writer.GetPendingBatches().Single().Messages.Select(m => m.Id).ToList();

        writer.MarkFlushed([ids[0]]);

        var remaining = writer.GetPendingBatches().Single().Messages;
        Assert.Equal(ids[1], Assert.Single(remaining).Id);
    }

    [Fact]
    public void ClearFlushedOnCommit_DropsFlushedMessagesPermanently()
    {
        var writer = CreateWriter();
        writer.Enqueue<ITestOutbox>(new SamplePayload());
        var id = writer.GetPendingBatches().Single().Messages.Single().Id;
        writer.MarkFlushed([id]);

        writer.ClearFlushedOnCommit();

        Assert.Empty(writer.GetPendingBatches());
    }

    [Fact]
    public void RestoreFlushedOnRollback_ReturnsFlushedMessagesToPending()
    {
        var writer = CreateWriter();
        writer.Enqueue<ITestOutbox>(new SamplePayload());
        var id = writer.GetPendingBatches().Single().Messages.Single().Id;
        writer.MarkFlushed([id]);

        writer.RestoreFlushedOnRollback();

        var restored = writer.GetPendingBatches().Single().Messages.Single();
        Assert.Equal(id, restored.Id);
    }

    [Fact]
    public void ClearPending_ClearsBothBuffers()
    {
        var writer = CreateWriter();
        writer.Enqueue<ITestOutbox>(new SamplePayload());
        var id = writer.GetPendingBatches().Single().Messages.Single().Id;
        writer.MarkFlushed([id]);

        writer.ClearPending();
        writer.RestoreFlushedOnRollback();

        Assert.Empty(writer.GetPendingBatches());
    }
}
