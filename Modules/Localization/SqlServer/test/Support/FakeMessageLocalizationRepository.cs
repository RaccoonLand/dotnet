using RaccoonLand.Modules.MessageLocalization.SQLServer.Data;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Support;

internal sealed class FakeMessageLocalizationRepository : IMessageLocalizationRepository
{
    public List<string> Calls { get; } = [];

    public Exception? EnsureSchemaException { get; set; }
    public Exception? EnsureApplicationException { get; set; }
    public Exception? LoadException { get; set; }
    public Exception? InsertMissingException { get; set; }

    public int ApplicationId { get; set; } = 7;
    public IReadOnlyList<LocalizationEntry> LoadResult { get; set; } = [];
    public IReadOnlyCollection<MissingKey>? LastInsertedKeys { get; private set; }
    public int? LastLoadApplicationId { get; private set; }

    public Func<CancellationToken, Task>? OnEnsureSchema { get; set; }
    public Func<CancellationToken, Task<int>>? OnEnsureApplication { get; set; }
    public Func<int, CancellationToken, Task<IReadOnlyList<LocalizationEntry>>>? OnLoad { get; set; }
    public Func<int, IReadOnlyCollection<MissingKey>, CancellationToken, Task>? OnInsertMissing { get; set; }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        Calls.Add(nameof(EnsureSchemaAsync));
        cancellationToken.ThrowIfCancellationRequested();
        if (EnsureSchemaException is not null)
        {
            throw EnsureSchemaException;
        }

        if (OnEnsureSchema is not null)
        {
            await OnEnsureSchema(cancellationToken);
        }
    }

    public async Task<int> EnsureApplicationAsync(CancellationToken cancellationToken)
    {
        Calls.Add(nameof(EnsureApplicationAsync));
        cancellationToken.ThrowIfCancellationRequested();
        if (EnsureApplicationException is not null)
        {
            throw EnsureApplicationException;
        }

        if (OnEnsureApplication is not null)
        {
            return await OnEnsureApplication(cancellationToken);
        }

        return ApplicationId;
    }

    public async Task<IReadOnlyList<LocalizationEntry>> LoadAsync(int applicationId, CancellationToken cancellationToken)
    {
        Calls.Add(nameof(LoadAsync));
        LastLoadApplicationId = applicationId;
        cancellationToken.ThrowIfCancellationRequested();
        if (LoadException is not null)
        {
            throw LoadException;
        }

        if (OnLoad is not null)
        {
            return await OnLoad(applicationId, cancellationToken);
        }

        return LoadResult;
    }

    public async Task InsertMissingAsync(
        int applicationId,
        IReadOnlyCollection<MissingKey> keys,
        CancellationToken cancellationToken)
    {
        Calls.Add(nameof(InsertMissingAsync));
        LastInsertedKeys = keys;
        cancellationToken.ThrowIfCancellationRequested();
        if (InsertMissingException is not null)
        {
            throw InsertMissingException;
        }

        if (OnInsertMissing is not null)
        {
            await OnInsertMissing(applicationId, keys, cancellationToken);
        }
    }
}
