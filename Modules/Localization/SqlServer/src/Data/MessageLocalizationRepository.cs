using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Configuration;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Data;

/// <summary>
/// Dapper-based data access for the shared localization database. Used only at startup and by the periodic
/// refresh worker, never on the request hot path (requests are served from the in-memory store).
/// </summary>
internal sealed class MessageLocalizationRepository : IMessageLocalizationRepository
{
    // SQL Server regular identifiers: letter/underscore, then letter/digit/underscore/@/#/$.
    private static readonly Regex SqlIdentifier = new(
        @"^[\p{L}_][\p{L}\p{Nd}_@#$]*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private const int MaxSqlIdentifierLength = 128;

    private readonly MessageLocalizationSqlServerOptions _options;
    private readonly string _schemaName;
    private readonly string _servicesTableName;
    private readonly string _applicationsTableName;
    private readonly string _messagesTableName;
    private readonly string _servicesTable;
    private readonly string _applicationsTable;
    private readonly string _messagesTable;

    public MessageLocalizationRepository(IOptions<MessageLocalizationSqlServerOptions> options)
    {
        _options = options.Value;
        _schemaName = ValidateIdentifier(_options.SchemaName, nameof(_options.SchemaName));
        _servicesTableName = ValidateIdentifier(_options.ServicesTableName, nameof(_options.ServicesTableName));
        _applicationsTableName = ValidateIdentifier(_options.ApplicationsTableName, nameof(_options.ApplicationsTableName));
        _messagesTableName = ValidateIdentifier(
            _options.MessageLocalizationsTableName,
            nameof(_options.MessageLocalizationsTableName));

        _servicesTable = Qualify(_schemaName, _servicesTableName);
        _applicationsTable = Qualify(_schemaName, _applicationsTableName);
        _messagesTable = Qualify(_schemaName, _messagesTableName);
    }

    /// <summary>Creates the schema and tables when missing (no-op if they already exist).</summary>
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        // CREATE SCHEMA must run in its own batch; build the identifier in C# to avoid fragile dynamic SQL.
        var quotedSchema = Quote(_schemaName);
        await ExecuteIgnoringObjectAlreadyExistsAsync(
            connection,
            """
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = @schema)
                EXEC sp_executesql @createSchemaSql;
            """,
            new
            {
                schema = _schemaName,
                createSchemaSql = $"CREATE SCHEMA {quotedSchema}",
            },
            cancellationToken);

        // OBJECT_ID arguments are parameters (not interpolated into N'...') so apostrophes cannot break the
        // literal. CREATE TABLE still uses bracket-quoted identifiers validated above.
        // Concurrent replicas may both pass OBJECT_ID IS NULL; treat 2714 (already exists) as success.
        var createTables =
            $"""
             IF OBJECT_ID(@ServicesObject, N'U') IS NULL
             CREATE TABLE {_servicesTable} (
                 [Id]           INT            IDENTITY (1, 1) NOT NULL PRIMARY KEY,
                 [Name]         NVARCHAR (128) NOT NULL UNIQUE,
                 [CreatedOnUtc] DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME())
             );

             IF OBJECT_ID(@ApplicationsObject, N'U') IS NULL
             CREATE TABLE {_applicationsTable} (
                 [Id]           INT            IDENTITY (1, 1) NOT NULL PRIMARY KEY,
                 [ServiceId]    INT            NOT NULL REFERENCES {_servicesTable} ([Id]),
                 [Name]         NVARCHAR (128) NOT NULL,
                 [CreatedOnUtc] DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
                 UNIQUE ([ServiceId], [Name])
             );

             IF OBJECT_ID(@MessagesObject, N'U') IS NULL
             CREATE TABLE {_messagesTable} (
                 [Id]                  BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
                 [ApplicationId]       INT            NOT NULL REFERENCES {_applicationsTable} ([Id]),
                 [Key]                 NVARCHAR (256) NOT NULL,
                 [Culture]             NVARCHAR (16)  NOT NULL,
                 [Value]               NVARCHAR (MAX) NOT NULL,
                 [RequiresTranslation] BIT            NOT NULL DEFAULT (0),
                 [CreatedOnUtc]        DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
                 [ModifiedOnUtc]       DATETIME2 (7)  NULL,
                 UNIQUE ([ApplicationId], [Key], [Culture])
             );
             """;

        await ExecuteIgnoringObjectAlreadyExistsAsync(
            connection,
            createTables,
            new
            {
                ServicesObject = $"{_schemaName}.{_servicesTableName}",
                ApplicationsObject = $"{_schemaName}.{_applicationsTableName}",
                MessagesObject = $"{_schemaName}.{_messagesTableName}",
            },
            cancellationToken);
    }

    /// <summary>
    /// Resolves the application id for the configured service/application, creating the rows when
    /// <see cref="MessageLocalizationSqlServerOptions.AutoRegisterApplication"/> is enabled.
    /// </summary>
    public async Task<int> EnsureApplicationAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        if (_options.AutoRegisterApplication)
        {
            await TryInsertIgnoringUniqueViolationAsync(
                connection,
                $"""
                 INSERT INTO {_servicesTable} ([Name])
                 SELECT @ServiceName
                 WHERE NOT EXISTS (SELECT 1 FROM {_servicesTable} WHERE [Name] = @ServiceName);
                 """,
                new { _options.ServiceName },
                cancellationToken);
        }

        var serviceId = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
            $"SELECT [Id] FROM {_servicesTable} WHERE [Name] = @ServiceName;",
            new { _options.ServiceName },
            cancellationToken: cancellationToken))
            ?? throw new InvalidOperationException(
                $"Localization service '{_options.ServiceName}' was not found. Create it or enable AutoRegisterApplication.");

        if (_options.AutoRegisterApplication)
        {
            await TryInsertIgnoringUniqueViolationAsync(
                connection,
                $"""
                 INSERT INTO {_applicationsTable} ([ServiceId], [Name])
                 SELECT @ServiceId, @ApplicationName
                 WHERE NOT EXISTS (SELECT 1 FROM {_applicationsTable} WHERE [ServiceId] = @ServiceId AND [Name] = @ApplicationName);
                 """,
                new { ServiceId = serviceId, _options.ApplicationName },
                cancellationToken);
        }

        var applicationId = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
            $"SELECT [Id] FROM {_applicationsTable} WHERE [ServiceId] = @ServiceId AND [Name] = @ApplicationName;",
            new { ServiceId = serviceId, _options.ApplicationName },
            cancellationToken: cancellationToken))
            ?? throw new InvalidOperationException(
                $"Localization application '{_options.ApplicationName}' was not found. Create it or enable AutoRegisterApplication.");

        return applicationId;
    }

    /// <summary>Loads every (key, culture) entry for the given application.</summary>
    public async Task<IReadOnlyList<LocalizationEntry>> LoadAsync(int applicationId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);

        var entries = await connection.QueryAsync<LocalizationEntry>(new CommandDefinition(
            $"""
             SELECT [Culture], [Key], [Value]
             FROM {_messagesTable}
             WHERE [ApplicationId] = @ApplicationId;
             """,
            new { ApplicationId = applicationId },
            cancellationToken: cancellationToken));

        return entries.AsList();
    }

    /// <summary>
    /// Persists missing keys as placeholders (<c>Value = Key</c>, <c>RequiresTranslation = 1</c>) so an admin
    /// can later supply the real translation. All keys are inserted with a single set-based statement
    /// (via <c>OPENJSON</c>) to avoid an N+1 round trip; existing rows are left untouched.
    /// </summary>
    public async Task InsertMissingAsync(int applicationId, IReadOnlyCollection<MissingKey> keys, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return;
        }

        var json = JsonSerializer.Serialize(keys);

        await using var connection = new SqlConnection(_options.ConnectionString);

        var sql =
            $"""
             INSERT INTO {_messagesTable} ([ApplicationId], [Key], [Culture], [Value], [RequiresTranslation])
             SELECT @ApplicationId, source.[Key], source.[Culture], source.[Key], 1
             FROM OPENJSON(@Json) WITH ([Key] NVARCHAR (256) '$.Key', [Culture] NVARCHAR (16) '$.Culture') AS source
             WHERE NOT EXISTS (
                 SELECT 1 FROM {_messagesTable} AS existing
                 WHERE existing.[ApplicationId] = @ApplicationId
                   AND existing.[Key] = source.[Key]
                   AND existing.[Culture] = source.[Culture]);
             """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { ApplicationId = applicationId, Json = json },
            cancellationToken: cancellationToken));
    }

    private static async Task ExecuteIgnoringObjectAlreadyExistsAsync(
        SqlConnection connection,
        string sql,
        object parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        }
        catch (SqlException exception) when (IsObjectAlreadyExists(exception))
        {
            // Another replica created the schema/table first; treat as success.
        }
    }

    private static async Task TryInsertIgnoringUniqueViolationAsync(
        SqlConnection connection,
        string sql,
        object parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        }
        catch (SqlException exception) when (IsUniqueViolation(exception))
        {
            // Another replica inserted the same row concurrently; treat as success and continue to SELECT.
        }
    }

    private static bool IsUniqueViolation(SqlException exception)
        => exception.Number is 2627 or 2601;

    /// <summary>SQL Server error 2714: there is already an object named … in the database.</summary>
    private static bool IsObjectAlreadyExists(SqlException exception)
        => exception.Number is 2714;

    private static string ValidateIdentifier(string identifier, string optionName)
    {
        if (string.IsNullOrWhiteSpace(identifier)
            || identifier.Length > MaxSqlIdentifierLength
            || !SqlIdentifier.IsMatch(identifier))
        {
            throw new InvalidOperationException(
                $"MessageLocalization option '{optionName}' value '{identifier}' is not a valid SQL identifier. " +
                $"Use at most {MaxSqlIdentifierLength} characters: letters, digits, underscore, @, #, or $ " +
                "(must start with a letter or underscore).");
        }

        return identifier;
    }

    private static string Qualify(string schema, string table) => $"{Quote(schema)}.{Quote(table)}";

    private static string Quote(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
}
