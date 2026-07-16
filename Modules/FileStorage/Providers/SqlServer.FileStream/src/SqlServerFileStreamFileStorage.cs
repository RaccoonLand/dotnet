using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.FileStorage.SqlServer.FileStream.Configuration;
using RaccoonLand.Modules.FileStorage.SqlServer.FileStream.Internal;

namespace RaccoonLand.Modules.FileStorage.SqlServer.FileStream;

internal sealed partial class SqlServerFileStreamFileStorage : IFileStorage
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly SqlServerFileStreamStorageOptions _options;
    private readonly FileStorageOptions _sharedOptions;
    private readonly string _qualifiedTableName;
    private readonly SemaphoreSlim _schemaLock = new(1, 1);
    private volatile bool _schemaReady;
    private FileStorageConfigurationException? _schemaConfigurationFailure;

    public SqlServerFileStreamFileStorage(
        IOptions<SqlServerFileStreamStorageOptions> options,
        IOptions<FileStorageOptions> sharedOptions)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new FileStorageConfigurationException(
                "SQL Server FILESTREAM storage is only supported on Windows hosts.");
        }

        _options = options.Value;
        _sharedOptions = sharedOptions.Value;
        ValidateIdentifiers(_options.SchemaName, _options.TableName, _options.ContentColumnName, _options.FileGroupName);
        _qualifiedTableName = $"[{_options.SchemaName}].[{_options.TableName}]";
    }

    public async Task<PutFileResult> PutAsync(PutFileRequest request, CancellationToken cancellationToken = default)
    {
        FileStorageGuards.ValidatePutRequest(request, _sharedOptions);
        await EnsureSchemaAsync(cancellationToken);

        var key = StorageKey.NormalizeOrGenerate(request.Key);
        var metadataJson = request.Metadata is null
            ? null
            : JsonSerializer.Serialize(request.Metadata, MetadataJsonOptions);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var storedCreatedAtUtc = await PrepareRowForPutAsync(
                connection,
                transaction,
                key,
                request.Mode,
                request.ContentType,
                metadataJson,
                cancellationToken);

            var (path, transactionContext) = await GetFileStreamAccessAsync(
                connection,
                transaction,
                key,
                cancellationToken);

            long length;
            string? checksum;

            await using (var sqlStream = new SqlFileStream(
                             path,
                             transactionContext,
                             FileAccess.Write,
                             FileOptions.Asynchronous | FileOptions.SequentialScan,
                             allocationSize: 0))
            {
                await using var hashingStream = new HashingWriteStream(sqlStream);
                var uploadStream = CreateLimitedUploadStream(request);
                await uploadStream.CopyToAsync(hashingStream, cancellationToken);
                await hashingStream.FlushAsync(cancellationToken);
                length = hashingStream.BytesWritten;
                checksum = hashingStream.GetChecksumHex();
            }

            await UpdateLengthAsync(connection, transaction, key, length, checksum, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new PutFileResult(new FileRef
            {
                Key = key,
                Version = checksum,
                Length = length,
                ContentType = request.ContentType,
                ChecksumSha256 = checksum,
                CreatedAtUtc = storedCreatedAtUtc,
            });
        }
        catch (FileStorageException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 && request.Mode is PutMode.CreateOnly)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new FileAlreadyExistsStorageException(key);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new FileStorageUnavailableException("SQL Server FILESTREAM upload failed.", ex);
        }
    }

    public async Task<OpenReadResult> OpenReadAsync(OpenReadRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        await EnsureSchemaAsync(cancellationToken);

        SqlConnection? connection = CreateConnection();
        SqlTransaction? transaction = null;
        SqlFileStream? sqlStream = null;

        try
        {
            await connection.OpenAsync(cancellationToken);
            transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            var (path, transactionContext, fileRef, _) = await GetFileStreamMetadataAsync(
                connection,
                transaction,
                key,
                cancellationToken);

            sqlStream = new SqlFileStream(
                path,
                transactionContext,
                FileAccess.Read,
                FileOptions.Asynchronous | FileOptions.SequentialScan,
                allocationSize: 0);

            var resultStream = new SqlFileStreamReadStream(connection, transaction, sqlStream);

            transaction = null;
            connection = null;
            sqlStream = null;

            return new OpenReadResult
            {
                Content = resultStream,
                File = fileRef,
            };
        }
        catch
        {
            await StorageDisposeHelper.DisposeQuietlyAsync(sqlStream).ConfigureAwait(false);
            await StorageDisposeHelper.DisposeQuietlyAsync(transaction).ConfigureAwait(false);
            await StorageDisposeHelper.DisposeQuietlyAsync(connection).ConfigureAwait(false);
            throw;
        }
    }

    private Stream CreateLimitedUploadStream(PutFileRequest request)
    {
        var effectiveMaxBytes = FileStorageGuards.ResolveEffectiveMaxUploadBytes(
            request.MaxUploadBytes,
            _sharedOptions.MaxUploadBytes);

        return effectiveMaxBytes is long maxBytes
            ? new MaxUploadLimitStream(request.Content, maxBytes)
            : request.Content;
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

        return await RowExistsAsync(connection, null, key, cancellationToken);
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
                Version = reader.IsDBNull(2) ? null : reader.GetString(2),
            },
            UserMetadata = metadata,
        };
    }

    private async Task<DateTimeOffset> PrepareRowForPutAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string key,
        PutMode mode,
        string? contentType,
        string? metadataJson,
        CancellationToken cancellationToken)
    {
        var rowExists = await RowExistsAsync(connection, transaction, key, cancellationToken);

        if (mode is PutMode.CreateOnly)
        {
            if (rowExists)
            {
                throw new FileAlreadyExistsStorageException(key);
            }

            var createdAtUtc = DateTimeOffset.UtcNow;
            await InsertRowAsync(connection, transaction, key, contentType, createdAtUtc, metadataJson, cancellationToken);
            return createdAtUtc;
        }

        if (mode is PutMode.Overwrite)
        {
            if (!rowExists)
            {
                throw new FileNotFoundStorageException(key);
            }

            var createdAtUtc = await GetCreatedAtUtcAsync(connection, transaction, key, cancellationToken)
                ?? throw new FileNotFoundStorageException(key);

            await UpdateRowMetadataAsync(connection, transaction, key, contentType, metadataJson, cancellationToken);
            return createdAtUtc;
        }

        if (rowExists)
        {
            var createdAtUtc = await GetCreatedAtUtcAsync(connection, transaction, key, cancellationToken)
                ?? DateTimeOffset.UtcNow;

            await UpdateRowMetadataAsync(connection, transaction, key, contentType, metadataJson, cancellationToken);
            return createdAtUtc;
        }

        try
        {
            var createdAtUtc = DateTimeOffset.UtcNow;
            await InsertRowAsync(connection, transaction, key, contentType, createdAtUtc, metadataJson, cancellationToken);
            return createdAtUtc;
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            return await RetryUpsertUpdateAsync(
                connection,
                transaction,
                key,
                contentType,
                metadataJson,
                ex,
                cancellationToken);
        }
    }

    private async Task<DateTimeOffset> RetryUpsertUpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string key,
        string? contentType,
        string? metadataJson,
        SqlException originalException,
        CancellationToken cancellationToken)
    {
        var createdAtUtc = await GetCreatedAtUtcAsync(connection, transaction, key, cancellationToken);
        if (createdAtUtc is null)
        {
            throw new FileStorageUnavailableException(
                "Concurrent upsert failed because another operation deleted the row during conflict resolution.",
                originalException);
        }

        await UpdateRowMetadataAsync(connection, transaction, key, contentType, metadataJson, cancellationToken);
        return createdAtUtc.Value;
    }

    private async Task<bool> RowExistsAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string key,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT 1 FROM {_qualifiedTableName} WHERE [Key] = @Key";
        command.Parameters.Add(new SqlParameter("@Key", key));
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    private async Task<DateTimeOffset?> GetCreatedAtUtcAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string key,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [CreatedAtUtc] FROM {_qualifiedTableName} WHERE [Key] = @Key";
        command.Parameters.Add(new SqlParameter("@Key", key));

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : (DateTimeOffset)result;
    }

    private async Task InsertRowAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string key,
        string? contentType,
        DateTimeOffset createdAtUtc,
        string? metadataJson,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"""
             INSERT INTO {_qualifiedTableName} ([Key], [ContentType], [Length], [ChecksumSha256], [CreatedAtUtc], [Metadata])
             VALUES (@Key, @ContentType, 0, NULL, @CreatedAtUtc, @Metadata)
             """;
        command.Parameters.Add(new SqlParameter("@Key", key));
        command.Parameters.Add(new SqlParameter("@ContentType", (object?)contentType ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@CreatedAtUtc", createdAtUtc));
        command.Parameters.Add(new SqlParameter("@Metadata", (object?)metadataJson ?? DBNull.Value));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateRowMetadataAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string key,
        string? contentType,
        string? metadataJson,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"""
             UPDATE {_qualifiedTableName}
             SET [ContentType] = @ContentType,
                 [Metadata] = @Metadata
             WHERE [Key] = @Key
             """;
        command.Parameters.Add(new SqlParameter("@Key", key));
        command.Parameters.Add(new SqlParameter("@ContentType", (object?)contentType ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@Metadata", (object?)metadataJson ?? DBNull.Value));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateLengthAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string key,
        long length,
        string? checksum,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"UPDATE {_qualifiedTableName} SET [Length] = @Length, [ChecksumSha256] = @ChecksumSha256 WHERE [Key] = @Key";
        command.Parameters.Add(new SqlParameter("@Key", key));
        command.Parameters.Add(new SqlParameter("@Length", length));
        command.Parameters.Add(new SqlParameter("@ChecksumSha256", (object?)checksum ?? DBNull.Value));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<(string Path, byte[] TransactionContext)> GetFileStreamAccessAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string key,
        CancellationToken cancellationToken)
    {
        var (path, context, _, _) = await GetFileStreamMetadataAsync(connection, transaction, key, cancellationToken);
        return (path, context);
    }

    private async Task<(string Path, byte[] TransactionContext, FileRef FileRef, Dictionary<string, string>? Metadata)> GetFileStreamMetadataAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string key,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"""
             SELECT TOP 1
                 [{_options.ContentColumnName}].PathName(),
                 GET_FILESTREAM(),
                 [ContentType],
                 [Length],
                 [ChecksumSha256],
                 [CreatedAtUtc],
                 [Metadata]
             FROM {_qualifiedTableName}
             WHERE [Key] = @Key
             """;
        command.Parameters.Add(new SqlParameter("@Key", key));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new FileNotFoundStorageException(key);
        }

        var path = reader.GetString(0);
        var sqlBytes = reader.GetSqlBytes(1);
        if (sqlBytes.IsNull)
        {
            throw new FileStorageUnavailableException("FILESTREAM transaction context was empty.");
        }

        var context = sqlBytes.Value;
        var fileRef = new FileRef
        {
            Key = key,
            ContentType = reader.IsDBNull(2) ? null : reader.GetString(2),
            Length = reader.GetInt64(3),
            ChecksumSha256 = reader.IsDBNull(4) ? null : reader.GetString(4),
            CreatedAtUtc = reader.GetFieldValue<DateTimeOffset>(5),
            Version = reader.IsDBNull(4) ? null : reader.GetString(4),
        };

        Dictionary<string, string>? metadata = null;
        if (!reader.IsDBNull(6))
        {
            metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(6), MetadataJsonOptions);
        }

        return (path, context, fileRef, metadata);
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
                    IF NOT EXISTS (SELECT 1 FROM sys.filegroups WHERE name = @FileGroupName)
                    BEGIN
                        THROW 50000, 'FILESTREAM filegroup was not found. Create it before enabling EnsureSchema.', 1;
                    END

                    IF SCHEMA_ID(N'{_options.SchemaName}') IS NULL
                        EXEC(N'CREATE SCHEMA [{_options.SchemaName}]');

                    IF OBJECT_ID(N'{_qualifiedTableName}', N'U') IS NULL
                    BEGIN
                        EXEC(N'CREATE TABLE {_qualifiedTableName} (
                            [Key] NVARCHAR(128) NOT NULL PRIMARY KEY,
                            [BlobId] UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL UNIQUE DEFAULT NEWSEQUENTIALID(),
                            [{_options.ContentColumnName}] VARBINARY(MAX) FILESTREAM NULL,
                            [ContentType] NVARCHAR(128) NULL,
                            [Length] BIGINT NOT NULL,
                            [ChecksumSha256] NVARCHAR(64) NULL,
                            [CreatedAtUtc] DATETIMEOFFSET NOT NULL,
                            [Metadata] NVARCHAR(MAX) NULL
                        ) FILESTREAM_ON [{_options.FileGroupName}]');
                    END
                    """;
                command.Parameters.Add(new SqlParameter("@FileGroupName", _options.FileGroupName));
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

    private static FileStorageException MapSchemaException(SqlException ex)
    {
        if (IsSchemaConfigurationError(ex))
        {
            if (ex.Number is 50000)
            {
                return new FileStorageConfigurationException(
                    "EnsureSchema is enabled but the configured FILESTREAM filegroup was not found. " +
                    "Create the filegroup before enabling EnsureSchema, or disable EnsureSchema and apply the schema through your migration tooling. " +
                    $"SQL error {ex.Number}: {ex.Message}");
            }

            return new FileStorageConfigurationException(
                "EnsureSchema is enabled but the SQL connection lacks permission to create the storage schema or table. " +
                "Disable EnsureSchema and apply the schema through your migration tooling, or grant the required DDL permissions. " +
                $"SQL error {ex.Number}: {ex.Message}");
        }

        return new FileStorageUnavailableException("SqlServer FILESTREAM schema initialization failed.", ex);
    }

    private static bool IsSchemaConfigurationError(SqlException ex)
    {
        foreach (SqlError error in ex.Errors)
        {
            if (error.Number is 50000 or 229 or 262 or 2760 or 15151 or 18456 or 4060)
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateIdentifiers(params string[] identifiers)
    {
        foreach (var identifier in identifiers)
        {
            if (!IdentifierPattern().IsMatch(identifier))
            {
                throw new FileStorageConfigurationException($"Invalid SQL identifier '{identifier}'.");
            }
        }
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierPattern();

    private sealed class HashingWriteStream : Stream
    {
        private readonly Stream _inner;
        private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        public HashingWriteStream(Stream inner) => _inner = inner;

        public long BytesWritten { get; private set; }

        public string GetChecksumHex() => Convert.ToHexString(_hash.GetHashAndReset()).ToLowerInvariant();

        public override bool CanRead => false;

        public override bool CanWrite => true;

        public override bool CanSeek => false;

        public override long Length => BytesWritten;

        public override long Position { get => BytesWritten; set => throw new NotSupportedException(); }

        public override void Flush() => _inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            _hash.AppendData(buffer, offset, count);
            _inner.Write(buffer, offset, count);
            BytesWritten += count;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _hash.AppendData(buffer, offset, count);
            await _inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
            BytesWritten += count;
        }

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            _hash.AppendData(buffer.Span);
            await _inner.WriteAsync(buffer, cancellationToken);
            BytesWritten += buffer.Length;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _hash.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
