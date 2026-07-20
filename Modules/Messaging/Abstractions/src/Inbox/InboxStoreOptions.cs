namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Options for locating the consumer inbox table.
/// </summary>
public sealed class InboxStoreOptions
{
    /// <summary>Default root configuration section name (<c>InboxStore</c>).</summary>
    public const string SectionName = "InboxStore";

    public string? Database { get; set; }

    public string Schema { get; set; } = "dbo";

    public string Table { get; set; } = "InboxEvent";

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
