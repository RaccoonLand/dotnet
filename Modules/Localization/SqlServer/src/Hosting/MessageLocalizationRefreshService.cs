using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Configuration;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Data;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Hosting;

/// <summary>
/// Loads all translations into the in-memory store at startup and refreshes them on a configurable interval.
/// Each cycle first persists any keys reported as missing, then reloads the snapshot so admin changes and
/// newly created placeholders become visible.
/// </summary>
internal sealed class MessageLocalizationRefreshService(
    IMessageLocalizationRepository repository,
    MessageLocalizationStore store,
    MissingKeyTracker missingKeys,
    IOptions<MessageLocalizationSqlServerOptions> options,
    ILogger<MessageLocalizationRefreshService> logger) : BackgroundService
{
    private readonly IMessageLocalizationRepository _repository = repository;
    private readonly MessageLocalizationStore _store = store;
    private readonly MissingKeyTracker _missingKeys = missingKeys;
    private readonly MessageLocalizationSqlServerOptions _options = options.Value;
    private readonly ILogger<MessageLocalizationRefreshService> _logger = logger;

    private int _applicationId;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Done before the host starts serving requests so the first lookup already sees data.
        try
        {
            if (_options.AutoCreateTables)
            {
                await _repository.EnsureSchemaAsync(cancellationToken);
            }

            _applicationId = await _repository.EnsureApplicationAsync(cancellationToken);
            await ReloadAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Initial load of message localizations failed; starting with an empty store and retrying on the next refresh.");
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.RefreshInterval <= TimeSpan.Zero)
        {
            return;
        }

        using var timer = new PeriodicTimer(_options.RefreshInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    if (_applicationId == 0)
                    {
                        if (_options.AutoCreateTables)
                        {
                            await _repository.EnsureSchemaAsync(stoppingToken);
                        }

                        _applicationId = await _repository.EnsureApplicationAsync(stoppingToken);
                    }

                    await PersistMissingKeysAsync(stoppingToken);
                    await ReloadAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Refreshing message localizations failed; keeping the previous snapshot.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected when the host shuts down while waiting for the next refresh tick.
        }
    }

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        var entries = await _repository.LoadAsync(_applicationId, cancellationToken);
        _store.Replace(entries);
        _logger.LogDebug("Loaded message localizations for {CultureCount} culture(s).", _store.CultureCount);
    }

    private async Task PersistMissingKeysAsync(CancellationToken cancellationToken)
    {
        var pending = _missingKeys.Drain();
        if (pending.Count == 0)
        {
            return;
        }

        try
        {
            await _repository.InsertMissingAsync(_applicationId, pending, cancellationToken);
            _logger.LogInformation("Persisted {Count} missing localization key(s) for admin review.", pending.Count);
        }
        catch
        {
            // Drain already removed these keys; put them back so the next cycle can retry.
            _missingKeys.Requeue(pending);
            throw;
        }
    }
}
