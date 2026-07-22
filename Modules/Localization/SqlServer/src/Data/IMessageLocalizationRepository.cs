using RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Data;

/// <summary>
/// Data access used at startup and by the refresh worker. Not used on the request hot path.
/// </summary>
internal interface IMessageLocalizationRepository
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken);

    Task<int> EnsureApplicationAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<LocalizationEntry>> LoadAsync(int applicationId, CancellationToken cancellationToken);

    Task InsertMissingAsync(int applicationId, IReadOnlyCollection<MissingKey> keys, CancellationToken cancellationToken);
}
