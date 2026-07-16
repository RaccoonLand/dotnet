using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.FileStorage.SqlServer.Configuration;

namespace RaccoonLand.Modules.FileStorage.SqlServer;

internal sealed partial class SqlServerFileStorage : IFileStorage
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly SqlServerStorageOptions _options;
    private readonly FileStorageOptions _sharedOptions;
    private readonly string _qualifiedTableName;
    private readonly SemaphoreSlim _schemaLock = new(1, 1);
    private volatile bool _schemaReady;
    private FileStorageConfigurationException? _schemaConfigurationFailure;

    public SqlServerFileStorage(
        IOptions<SqlServerStorageOptions> options,
        IOptions<FileStorageOptions> sharedOptions)
    {
        _options = options.Value;
        _sharedOptions = sharedOptions.Value;
        ValidateIdentifier(_options.SchemaName);
        ValidateIdentifier(_options.TableName);
        _qualifiedTableName = $"[{_options.SchemaName}].[{_options.TableName}]";
    }

    public async Task<PutFileResult> PutAsync(PutFileRequest request, CancellationToken cancellationToken = default)
    {
        FileStorageGuards.ValidatePutRequest(request, _sharedOptions);
        await EnsureSchemaAsync(cancellationToken);

        var key = StorageKey.NormalizeOrGenerate(request.Key);
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        if (request.Mode is PutMode.CreateOnly)
        {
            await using var existsCommand = connection.CreateCommand();
            existsCommand.CommandText = $"SELECT 1 FROM {_qualifiedTableName} WHERE [Key] = @Key";
            existsCommand.Parameters.Add(new SqlParameter("@Key", key));

            if (await existsCommand.ExecuteScalarAsync(cancellationToken) is not null)
            {
                throw new FileAlreadyExistsStorageException(key);
            }
        }

        await using var memory = new MemoryStream();
        await request.Content.CopyToAsync(memory, cancellationToken);
        var content = memory.ToArray();

        if (_sharedOptions.MaxUploadBytes is long maxBytes && content.LongLength > maxBytes)
        {
            throw new FileStorageValidationException($"Upload exceeds the configured limit of {maxBytes} bytes.");
        }

        var createdAt = DateTimeOffset.UtcNow;
        var metadataJson = request.Metadata is null
            ? null
            : JsonSerializer.Serialize(request.Metadata, MetadataJsonOptions);

        await using var command = connection.CreateCommand();
        command.CommandText = request.Mode switch
        {
            PutMode.CreateOnly => InsertSql(),
            PutMode.Overwrite => UpdateSql(),
            _ => UpsertSql(),
        };

        AddPutParameters(command, key, content, request.ContentType, createdAt, metadataJson);

        DateTimeOffset storedCreatedAtUtc;
        try
        {
            storedCreatedAtUtc = await ExecutePutWithOutputAsync(command, request.Mode, key, cancellationToken);
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 && request.Mode is PutMode.CreateOnly)
        {
            throw new FileAlreadyExistsStorageException(key);
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 && request.Mode is PutMode.Upsert)
        {
            storedCreatedAtUtc = await RetryUpsertUpdateAsync(
                connection,
                key,
                content,
                request.ContentType,
                createdAt,
                metadataJson,
                ex,
                cancellationToken);
        }
        catch (SqlException ex)
        {
            throw new FileStorageUnavailableException("SqlServer file storage operation failed.", ex);
        }

        return new PutFileResult(new FileRef
        {
            Key = key,
            Length = content.LongLength,
            ContentType = request.ContentType,
            CreatedAtUtc = storedCreatedAtUtc,
        });
    }

    public async Task<OpenReadResult> OpenReadAsync(OpenReadRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        await EnsureSchemaAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT [Content], [ContentType], [Length], [ChecksumSha256], [CreatedAtUtc] FROM {_qualifiedTableName} WHERE [Key] = @Key";
        command.Parameters.Add(new SqlParameter("@Key", key));

        await using var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new FileNotFoundStorageException(key);
        }

        var stream = new MemoryStream();
        await using (var sqlStream = reader.GetStream(0))
        {
            await sqlStream.CopyToAsync(stream, cancellationToken);
        }

        stream.Position = 0;

        return new OpenReadResult
        {
            Content = stream,
            File = new FileRef
            {
                Key = key,
                ContentType = reader.IsDBNull(1) ? null : reader.GetString(1),
                Length = reader.GetInt64(2),
                ChecksumSha256 = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAtUtc = reader.GetFieldValue<DateTimeOffset>(4),
            },
        };
    }

    public async Task DeleteAsync(DeleteFileRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        await EnsureSchemaAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {_qualifiedTableName} WHERE [Key] = @Key";
        command.Parameters.Add(new SqlParameter("@Key", key));

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected == 0 && !request.IgnoreNotFound)
        {
            throw new FileNotFoundStorageException(key);
        }
    }

    public async Task<bool> ExistsAsync(ExistsFileRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        await EnsureSchemaAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT 1 FROM {_qualifiedTableName} WHERE [Key] = @Key";
        command.Parameters.Add(new SqlParameter("@Key", key));

        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<FileMetadata?> GetMetadataAsync(GetMetadataRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        await EnsureSchemaAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT [ContentType], [Length], [ChecksumSha256], [CreatedAtUtc], [Metadata] FROM {_qualifiedTableName} WHERE [Key] = @Key";
        command.Parameters.Add(new SqlParameter("@Key", key));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        Dictionary<string, string>? metadata = null;
        if (!reader.IsDBNull(4))
        {
            metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(4), MetadataJsonOptions);
        }

        return new FileMetadata
        {
            File = new FileRef
            {
                Key = key,
                ContentType = reader.IsDBNull(0) ? null : reader.GetString(0),
                Length = reader.GetInt64(1),
                ChecksumSha256 = reader.IsDBNull(2) ? null : reader.GetString(2),
                CreatedAtUtc = reader.GetFieldValue<DateTimeOffset>(3),
            },
            UserMetadata = metadata,
        };
    }

    private SqlConnection CreateConnection() => new(_options.ConnectionString);

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (!_options.EnsureSchema || _schemaReady)
        {
            return;
        }

        if (_schemaConfigurationFailure is not null)
        {
            throw _schemaConfigurationFailure;
        }

        await _schemaLock.WaitAsync(cancellationToken);
        try
        {
            if (_schemaReady)
            {
                return;
            }

            if (_schemaConfigurationFailure is not null)
            {
                throw _schemaConfigurationFailure;
            }

            try
            {
                await using var connection = CreateConnection();
                await connection.OpenAsync(cancellationToken);

                await using var command = connection.CreateCommand();
                command.CommandText = $"""
                    IF SCHEMA_ID(N'{_options.SchemaName}') IS NULL
                        EXEC(N'CREATE SCHEMA [{_options.SchemaName}]');

                    IF OBJECT_ID(N'{_qualifiedTableName}', N'U') IS NULL
                    BEGIN
                        CREATE TABLE {_qualifiedTableName} (
                            [Key] NVARCHAR(128) NOT NULL PRIMARY KEY,
                            [Content] VARBINARY(MAX) NOT NULL,
                            [ContentType] NVARCHAR(128) NULL,
                            [Length] BIGINT NOT NULL,
                            [ChecksumSha256] NVARCHAR(64) NULL,
                            [CreatedAtUtc] DATETIMEOFFSET NOT NULL,
                            [Metadata] NVARCHAR(MAX) NULL
                        );
                    END
                    """;

                await command.ExecuteNonQueryAsync(cancellationToken);
                _schemaReady = true;
            }
            catch (SqlException ex)
            {
                var mapped = MapSchemaException(ex);
                if (mapped is FileStorageConfigurationException configurationException)
                {
                    _schemaConfigurationFailure = configurationException;
                }

                throw mapped;
            }
        }
        finally
        {
            _schemaLock.Release();
        }
    }

    private static async Task<DateTimeOffset> ExecutePutWithOutputAsync(
        SqlCommand command,
        PutMode mode,
        string key,
        CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        do
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                return reader.GetFieldValue<DateTimeOffset>(0);
            }
        }
        while (await reader.NextResultAsync(cancellationToken));

        if (mode is PutMode.Overwrite)
        {
            throw new FileNotFoundStorageException(key);
        }

        throw new FileStorageUnavailableException("SqlServer put operation did not return the stored created timestamp.");
    }

    private async Task<DateTimeOffset> RetryUpsertUpdateAsync(
        SqlConnection connection,
        string key,
        byte[] content,
        string? contentType,
        DateTimeOffset createdAt,
        string? metadataJson,
        SqlException originalException,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = UpdateSql();
        AddPutParameters(command, key, content, contentType, createdAt, metadataJson);

        try
        {
            return await ExecutePutWithOutputAsync(command, PutMode.Overwrite, key, cancellationToken);
        }
        catch (FileNotFoundStorageException)
        {
            throw new FileStorageUnavailableException(
                "Concurrent upsert failed because another operation deleted the row during conflict resolution.",
                originalException);
        }
        catch (SqlException ex)
        {
            throw new FileStorageUnavailableException("SqlServer file storage operation failed.", ex);
        }
    }

    private static void AddPutParameters(
        SqlCommand command,
        string key,
        byte[] content,
        string? contentType,
        DateTimeOffset createdAt,
        string? metadataJson)
    {
        command.Parameters.Add(new SqlParameter("@Key", key));
        command.Parameters.Add(new SqlParameter("@Content", content));
        command.Parameters.Add(new SqlParameter("@ContentType", (object?)contentType ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@Length", content.LongLength));
        command.Parameters.Add(new SqlParameter("@ChecksumSha256", DBNull.Value));
        command.Parameters.Add(new SqlParameter("@CreatedAtUtc", createdAt));
        command.Parameters.Add(new SqlParameter("@Metadata", (object?)metadataJson ?? DBNull.Value));
    }

    private string InsertSql() =>
        $"""
         INSERT INTO {_qualifiedTableName} ([Key], [Content], [ContentType], [Length], [ChecksumSha256], [CreatedAtUtc], [Metadata])
         OUTPUT INSERTED.[CreatedAtUtc]
         VALUES (@Key, @Content, @ContentType, @Length, @ChecksumSha256, @CreatedAtUtc, @Metadata)
         """;

    private string UpdateSql() =>
        $"""
         UPDATE {_qualifiedTableName}
         SET [Content] = @Content,
             [ContentType] = @ContentType,
             [Length] = @Length,
             [ChecksumSha256] = @ChecksumSha256,
             [Metadata] = @Metadata
         OUTPUT INSERTED.[CreatedAtUtc]
         WHERE [Key] = @Key
         """;

    private string UpsertSql() =>
        $"""
         UPDATE {_qualifiedTableName}
         SET [Content] = @Content,
             [ContentType] = @ContentType,
             [Length] = @Length,
             [ChecksumSha256] = @ChecksumSha256,
             [Metadata] = @Metadata
         OUTPUT INSERTED.[CreatedAtUtc]
         WHERE [Key] = @Key;

         IF @@ROWCOUNT = 0
         BEGIN
             INSERT INTO {_qualifiedTableName} ([Key], [Content], [ContentType], [Length], [ChecksumSha256], [CreatedAtUtc], [Metadata])
             OUTPUT INSERTED.[CreatedAtUtc]
             VALUES (@Key, @Content, @ContentType, @Length, @ChecksumSha256, @CreatedAtUtc, @Metadata);
         END
         """;

    private static FileStorageException MapSchemaException(SqlException ex)
    {
        if (IsSchemaConfigurationError(ex))
        {
            return new FileStorageConfigurationException(
                "EnsureSchema is enabled but the SQL connection lacks permission to create the storage schema or table. " +
                "Disable EnsureSchema and apply the schema through your migration tooling, or grant the required DDL permissions. " +
                $"SQL error {ex.Number}: {ex.Message}");
        }

        return new FileStorageUnavailableException("SqlServer schema initialization failed.", ex);
    }

    private static bool IsSchemaConfigurationError(SqlException ex)
    {
        foreach (SqlError error in ex.Errors)
        {
            if (error.Number is 229 or 262 or 2760 or 15151 or 18456 or 4060)
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateIdentifier(string identifier)
    {
        if (!IdentifierPattern().IsMatch(identifier))
        {
            throw new FileStorageConfigurationException($"Invalid SQL identifier '{identifier}'.");
        }
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierPattern();
}
