namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

/// <summary>
/// Storage location for a registered outbox channel on SQL Server. The table may live in the same database as
/// the command data, or in another database on the <em>same SQL Server instance</em> (three-part naming).
/// Cross-instance / DTC scenarios are intentionally not supported.
/// </summary>
public sealed class OutboxChannelOptions
{
    /// <summary>
    /// Optional database name. When set, the table is referenced with three-part naming so the write can
    /// stay inside the same transaction as <c>SaveChanges</c>.
    /// </summary>
    public string? Database { get; set; }

    public string Schema { get; set; } = "dbo";

    public string Table { get; set; } = string.Empty;

    /// <summary>
    /// Validates <see cref="Schema"/>, <see cref="Table"/>, and (when set) <see cref="Database"/> as
    /// single-part T-SQL identifiers. Called by
    /// <see cref="OutboxChannelRegistry.Register(Type, OutboxChannelOptions)"/> so misconfiguration fails at
    /// registration instead of producing an opaque SQL error on the first save.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Any of the fields is null / empty / whitespace or not a valid single-part identifier
    /// (see <see cref="SqlServerIdentifier"/>).
    /// </exception>
    public void EnsureValid()
    {
        SqlServerIdentifier.Validate(Schema, nameof(Schema));
        SqlServerIdentifier.Validate(Table, nameof(Table));
        if (Database is not null)
        {
            SqlServerIdentifier.Validate(Database, nameof(Database));
        }
    }

    /// <summary>Builds the fully-qualified, bracket-quoted table name used in INSERT statements.</summary>
    public string QualifiedTableName => string.IsNullOrWhiteSpace(Database)
        ? $"{SqlServerIdentifier.QuotePart(Schema)}.{SqlServerIdentifier.QuotePart(Table)}"
        : $"{SqlServerIdentifier.QuotePart(Database!)}.{SqlServerIdentifier.QuotePart(Schema)}.{SqlServerIdentifier.QuotePart(Table)}";
}
