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

    /// <summary>Builds the fully-qualified, bracket-quoted table name used in INSERT statements.</summary>
    public string QualifiedTableName => string.IsNullOrWhiteSpace(Database)
        ? $"[{Schema}].[{Table}]"
        : $"[{Database}].[{Schema}].[{Table}]";
}
