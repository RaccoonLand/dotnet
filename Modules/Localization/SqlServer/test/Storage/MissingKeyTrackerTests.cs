using RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Storage;

public sealed class MissingKeyTrackerTests
{
    [Fact]
    public void Constructor_WhenCapacityNonPositive_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MissingKeyTracker(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MissingKeyTracker(-1));
    }

    [Fact]
    public void Default_UsesDefaultCapacity()
    {
        var tracker = new MissingKeyTracker();
        Assert.Equal(MissingKeyTracker.DefaultCapacity, tracker.Capacity);
    }

    [Fact]
    public void Report_DeduplicatesUntilDrain()
    {
        var tracker = new MissingKeyTracker();
        tracker.Report("en-US", "K");
        tracker.Report("en-US", "K");
        tracker.Report("en-US", "K");

        var drain = tracker.Drain();

        Assert.Single(drain.Keys);
        Assert.Contains(drain.Keys, k => k.Key == "K" && k.Culture == "en-US");
        Assert.Equal(0, drain.DroppedSinceLastDrain);
    }

    [Fact]
    public void Drain_ReturnsEmptyResult_WhenNothingReported()
    {
        var tracker = new MissingKeyTracker();

        var drain = tracker.Drain();

        Assert.Empty(drain.Keys);
        Assert.Equal(0, drain.DroppedSinceLastDrain);
    }

    [Fact]
    public void Report_WhenCapacityReached_DropsNewKeysAndCountsThem()
    {
        var tracker = new MissingKeyTracker(capacity: 2);

        tracker.Report("en-US", "A");
        tracker.Report("en-US", "B");
        tracker.Report("en-US", "C"); // dropped
        tracker.Report("en-US", "D"); // dropped

        var drain = tracker.Drain();

        Assert.Equal(2, drain.Keys.Count);
        Assert.Contains(drain.Keys, k => k.Key == "A");
        Assert.Contains(drain.Keys, k => k.Key == "B");
        Assert.DoesNotContain(drain.Keys, k => k.Key == "C");
        Assert.DoesNotContain(drain.Keys, k => k.Key == "D");
        Assert.Equal(2, drain.DroppedSinceLastDrain);
    }

    [Fact]
    public void Report_WhenAlreadyTracked_NeverDropsEvenAtCapacity()
    {
        var tracker = new MissingKeyTracker(capacity: 1);
        tracker.Report("en-US", "A");

        // Duplicate of a tracked key must never be counted as a drop.
        tracker.Report("en-US", "A");
        tracker.Report("en-US", "A");

        var drain = tracker.Drain();

        Assert.Single(drain.Keys);
        Assert.Equal(0, drain.DroppedSinceLastDrain);
    }

    [Fact]
    public void Drain_ResetsDropCounter()
    {
        var tracker = new MissingKeyTracker(capacity: 1);
        tracker.Report("en-US", "A");
        tracker.Report("en-US", "B"); // dropped

        var first = tracker.Drain();
        Assert.Equal(1, first.DroppedSinceLastDrain);

        var second = tracker.Drain();
        Assert.Equal(0, second.DroppedSinceLastDrain);
    }

    [Fact]
    public void Requeue_NullKeys_Throws()
    {
        var tracker = new MissingKeyTracker();
        Assert.Throws<ArgumentNullException>(() => tracker.Requeue(null!));
    }

    [Fact]
    public void Requeue_EmptyKeys_IsNoop()
    {
        var tracker = new MissingKeyTracker();
        tracker.Requeue([]);

        var drain = tracker.Drain();
        Assert.Empty(drain.Keys);
        Assert.Equal(0, drain.DroppedSinceLastDrain);
    }

    [Fact]
    public void Requeue_MergesWithConcurrentReports_WithoutDuplicates()
    {
        var tracker = new MissingKeyTracker();
        tracker.Report("en-US", "SharedKey");

        tracker.Requeue(
        [
            new MissingKey("en-US", "SharedKey"),
            new MissingKey("en-US", "RequeueOnly"),
        ]);

        var drain = tracker.Drain();

        Assert.Equal(2, drain.Keys.Count);
        Assert.Contains(drain.Keys, k => k.Key == "SharedKey");
        Assert.Contains(drain.Keys, k => k.Key == "RequeueOnly");
    }

    [Fact]
    public void Requeue_RespectsCapacity_AndCountsDrops()
    {
        var tracker = new MissingKeyTracker(capacity: 2);
        tracker.Report("en-US", "A");

        tracker.Requeue(
        [
            new MissingKey("en-US", "A"), // duplicate — not a drop
            new MissingKey("en-US", "B"), // fits — becomes 2/2
            new MissingKey("en-US", "C"), // dropped — capacity reached
            new MissingKey("en-US", "D"), // dropped
        ]);

        var drain = tracker.Drain();

        Assert.Equal(2, drain.Keys.Count);
        Assert.Contains(drain.Keys, k => k.Key == "A");
        Assert.Contains(drain.Keys, k => k.Key == "B");
        Assert.Equal(2, drain.DroppedSinceLastDrain);
    }

    [Fact]
    public void CultureAndKey_AreCaseSensitive_ForDedup()
    {
        var tracker = new MissingKeyTracker();
        tracker.Report("en-US", "Key");
        tracker.Report("EN-us", "Key");
        tracker.Report("en-US", "key");

        var drain = tracker.Drain();

        Assert.Equal(3, drain.Keys.Count);
    }
}
