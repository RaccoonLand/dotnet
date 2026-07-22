using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Hosting;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Support;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Hosting;

public sealed class MessageLocalizationRefreshServiceTests
{
    [Fact]
    public async Task StartAsync_WithAutoCreateTables_RunsSchemaThenApplicationThenLoad()
    {
        var repository = new FakeMessageLocalizationRepository
        {
            LoadResult = [new LocalizationEntry("en-US", "K", "V")],
        };
        var store = new MessageLocalizationStore();
        var service = LocalizationTestHelpers.CreateRefreshService(
            repository,
            store,
            options: LocalizationTestHelpers.ValidOptions(o =>
            {
                o.AutoCreateTables = true;
                o.RefreshInterval = TimeSpan.Zero;
            }));

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(
            [
                nameof(FakeMessageLocalizationRepository.EnsureSchemaAsync),
                nameof(FakeMessageLocalizationRepository.EnsureApplicationAsync),
                nameof(FakeMessageLocalizationRepository.LoadAsync),
            ],
            repository.Calls);
        Assert.True(store.TryGet("en-US", "K", out var value));
        Assert.Equal("V", value);
    }

    [Fact]
    public async Task StartAsync_WithoutAutoCreateTables_SkipsSchema()
    {
        var repository = new FakeMessageLocalizationRepository();
        var service = LocalizationTestHelpers.CreateRefreshService(
            repository,
            options: LocalizationTestHelpers.ValidOptions(o =>
            {
                o.AutoCreateTables = false;
                o.RefreshInterval = TimeSpan.Zero;
            }));

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.DoesNotContain(nameof(FakeMessageLocalizationRepository.EnsureSchemaAsync), repository.Calls);
        Assert.Equal(
            [
                nameof(FakeMessageLocalizationRepository.EnsureApplicationAsync),
                nameof(FakeMessageLocalizationRepository.LoadAsync),
            ],
            repository.Calls);
    }

    [Fact]
    public async Task StartAsync_WhenTransientError_ContinuesWithEmptyStore()
    {
        var repository = new FakeMessageLocalizationRepository
        {
            EnsureApplicationException = new InvalidOperationException("db down"),
        };
        var store = new MessageLocalizationStore();
        Assert.Equal(0, store.CultureCount);

        var service = LocalizationTestHelpers.CreateRefreshService(
            repository,
            store,
            options: LocalizationTestHelpers.ValidOptions(o => o.RefreshInterval = TimeSpan.Zero));

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, store.CultureCount);
        Assert.DoesNotContain(nameof(FakeMessageLocalizationRepository.LoadAsync), repository.Calls);
    }

    [Fact]
    public async Task StartAsync_WhenCanceled_RethrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var repository = new FakeMessageLocalizationRepository
        {
            EnsureApplicationException = new OperationCanceledException(cts.Token),
        };
        var service = LocalizationTestHelpers.CreateRefreshService(repository);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.StartAsync(cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshIntervalNonPositive_DoesNotRefresh()
    {
        var repository = new FakeMessageLocalizationRepository();
        var service = LocalizationTestHelpers.CreateRefreshService(
            repository,
            options: LocalizationTestHelpers.ValidOptions(o => o.RefreshInterval = TimeSpan.Zero));

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, repository.Calls.Count(c => c == nameof(FakeMessageLocalizationRepository.LoadAsync)));
        Assert.DoesNotContain(nameof(FakeMessageLocalizationRepository.InsertMissingAsync), repository.Calls);
    }

    [Fact]
    public async Task RefreshCycle_WhenStartupFailed_RetriesEnsureApplicationThenPersistThenLoad()
    {
        var repository = new FakeMessageLocalizationRepository
        {
            EnsureApplicationException = new InvalidOperationException("startup db down"),
            ApplicationId = 11,
            LoadResult =
            [
                new LocalizationEntry("en-US", "Old", "gone"),
                new LocalizationEntry("en-US", "New", "from-refresh"),
            ],
        };
        var store = new MessageLocalizationStore();
        var missingKeys = new MissingKeyTracker();
        missingKeys.Report("en-US", "PendingKey");

        var service = LocalizationTestHelpers.CreateRefreshService(
            repository,
            store,
            missingKeys,
            LocalizationTestHelpers.ValidOptions(o =>
            {
                o.AutoCreateTables = false;
                o.RefreshInterval = TimeSpan.FromMilliseconds(20);
            }));

        await service.StartAsync(CancellationToken.None);
        Assert.Equal(0, store.CultureCount);
        Assert.DoesNotContain(nameof(FakeMessageLocalizationRepository.LoadAsync), repository.Calls);

        // Allow the refresh loop to recover after startup failure.
        repository.EnsureApplicationException = null;
        repository.LoadResult =
        [
            new LocalizationEntry("en-US", "New", "from-refresh"),
            new LocalizationEntry("en-US", "PendingKey", "PendingKey"),
        ];

        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline
               && !store.TryGet("en-US", "New", out _))
        {
            await Task.Delay(20);
        }

        await service.StopAsync(CancellationToken.None);

        // First EnsureApplication is the failed startup call; find the successful recovery sequence.
        var ensureIndexes = repository.Calls
            .Select((c, i) => (c, i))
            .Where(x => x.c == nameof(FakeMessageLocalizationRepository.EnsureApplicationAsync))
            .Select(x => x.i)
            .ToList();
        Assert.True(ensureIndexes.Count >= 2, "Expected startup attempt plus refresh retry.");

        var recovery = repository.Calls.Skip(ensureIndexes[1]).Take(3).ToArray();
        Assert.Equal(
            [
                nameof(FakeMessageLocalizationRepository.EnsureApplicationAsync),
                nameof(FakeMessageLocalizationRepository.InsertMissingAsync),
                nameof(FakeMessageLocalizationRepository.LoadAsync),
            ],
            recovery);

        Assert.NotNull(repository.LastInsertedKeys);
        Assert.Contains(repository.LastInsertedKeys, k => k.Key == "PendingKey");
        Assert.True(store.TryGet("en-US", "New", out var value));
        Assert.Equal("from-refresh", value);
        Assert.False(store.TryGet("en-US", "Old", out _));
    }

    [Fact]
    public async Task RefreshCycle_PersistsMissingKeysThenReloads()
    {
        var repository = new FakeMessageLocalizationRepository
        {
            ApplicationId = 3,
            LoadResult = [new LocalizationEntry("en-US", "After", "reload")],
        };
        var store = new MessageLocalizationStore();
        var missingKeys = new MissingKeyTracker();
        missingKeys.Report("en-US", "Missing");

        var service = LocalizationTestHelpers.CreateRefreshService(
            repository,
            store,
            missingKeys,
            LocalizationTestHelpers.ValidOptions(o => o.RefreshInterval = TimeSpan.FromMilliseconds(20)));

        await service.StartAsync(CancellationToken.None);

        // Wait for at least one refresh tick after startup load.
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline
               && repository.Calls.Count(c => c == nameof(FakeMessageLocalizationRepository.InsertMissingAsync)) == 0)
        {
            await Task.Delay(20);
        }

        await service.StopAsync(CancellationToken.None);

        Assert.Contains(nameof(FakeMessageLocalizationRepository.InsertMissingAsync), repository.Calls);
        Assert.NotNull(repository.LastInsertedKeys);
        Assert.Contains(repository.LastInsertedKeys, k => k.Key == "Missing");

        var insertIndex = repository.Calls.IndexOf(nameof(FakeMessageLocalizationRepository.InsertMissingAsync));
        var loadAfterInsert = repository.Calls
            .Select((c, i) => (c, i))
            .Where(x => x.c == nameof(FakeMessageLocalizationRepository.LoadAsync) && x.i > insertIndex)
            .ToList();
        Assert.NotEmpty(loadAfterInsert);
        Assert.True(store.TryGet("en-US", "After", out var value));
        Assert.Equal("reload", value);
    }

    [Fact]
    public async Task RefreshCycle_WhenPersistFails_RequeuesMissingKeysAndKeepsSnapshot()
    {
        var repository = new FakeMessageLocalizationRepository
        {
            InsertMissingException = new InvalidOperationException("persist failed"),
            LoadResult = [new LocalizationEntry("en-US", "Kept", "snapshot")],
        };
        var store = new MessageLocalizationStore();
        var missingKeys = new MissingKeyTracker();

        var service = LocalizationTestHelpers.CreateRefreshService(
            repository,
            store,
            missingKeys,
            LocalizationTestHelpers.ValidOptions(o => o.RefreshInterval = TimeSpan.FromMilliseconds(20)));

        await service.StartAsync(CancellationToken.None);
        Assert.True(store.TryGet("en-US", "Kept", out _));

        missingKeys.Report("en-US", "RetryMe");

        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline
               && repository.Calls.Count(c => c == nameof(FakeMessageLocalizationRepository.InsertMissingAsync)) == 0)
        {
            await Task.Delay(20);
        }

        await service.StopAsync(CancellationToken.None);

        Assert.Contains(nameof(FakeMessageLocalizationRepository.InsertMissingAsync), repository.Calls);
        Assert.True(store.TryGet("en-US", "Kept", out var kept));
        Assert.Equal("snapshot", kept);

        var requeued = missingKeys.Drain();
        Assert.Contains(requeued, k => k.Key == "RetryMe");
    }
}
