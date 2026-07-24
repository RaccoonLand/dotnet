namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

/// <summary>
/// Configuration for where outbox events are written. The outbox table may live in the same
/// database as the command data, or in a separate database on the <em>same SQL Server instance</em>
/// (referenced via three-part naming). Cross-instance / DTC scenarios are intentionally not supported.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Optional database name. When set, the table is referenced with three-part naming
    /// (<c>[Database].[Schema].[Table]</c>) so the write can stay inside the same transaction
    /// as <c>SaveChanges</c> without DTC. When null, the current database is used.
    /// </summary>
    public string? Database { get; set; }

    public string Schema { get; set; } = "dbo";

    public string Table { get; set; } = "OutboxEvent";

    /// <summary>
    /// Validates <see cref="Schema"/>, <see cref="Table"/>, and (when set) <see cref="Database"/> as
    /// single-part T-SQL identifiers. Called from
    /// <c>AddRaccoonLandCommandInterceptors</c> so misconfiguration fails at startup instead of
    /// producing an opaque SQL error on the first save.
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

    /// <summary>Builds the fully-qualified, bracket-quoted table name used in the INSERT statement.</summary>
    public string QualifiedTableName => Database is null
        ? $"{SqlServerIdentifier.QuotePart(Schema)}.{SqlServerIdentifier.QuotePart(Table)}"
        : $"{SqlServerIdentifier.QuotePart(Database)}.{SqlServerIdentifier.QuotePart(Schema)}.{SqlServerIdentifier.QuotePart(Table)}";
}
