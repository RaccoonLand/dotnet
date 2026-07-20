namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Options for locating the aggregate-event outbox table when reading/marking rows.
/// Must match the write-side <c>OutboxOptions</c> used by
/// <c>OutboxSaveChangesInterceptor</c> (same database/schema/table).
/// </summary>
public sealed class OutboxEventStoreOptions
{
    /// <summary>Default root configuration section name (<c>OutboxEventStore</c>).</summary>
    public const string SectionName = "OutboxEventStore";

    /// <summary>
    /// Optional database name for three-part naming. When null, the connection's current database is used.
    /// </summary>
    public string? Database { get; set; }

    public string Schema { get; set; } = "dbo";

    public string Table { get; set; } = "OutboxEvent";

    /// <summary>
    /// Builds the fully-qualified, bracket-quoted table name used in SQL statements.
    /// Identifiers are validated via <see cref="SqlIdentifier"/>.
    /// </summary>
    public string QualifiedTableName
    {
        get
        {
            var schema = SqlIdentifier.Require(Schema, nameof(Schema));
            var table = SqlIdentifier.Require(Table, nameof(Table));
            if (Database is null)
            {
                return $"[{schema}].[{table}]";
            }

            var database = SqlIdentifier.Require(Database, nameof(Database));
            return $"[{database}].[{schema}].[{table}]";
        }
    }
}
