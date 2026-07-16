using Microsoft.Data.SqlClient;

namespace RaccoonLand.Modules.FileStorage.SqlServer.FileStream.Internal;

/// <summary>Keeps the SQL transaction open until the FILESTREAM read completes.</summary>
internal sealed class SqlFileStreamReadStream : Stream
{
    private readonly SqlConnection _connection;
    private readonly SqlTransaction _transaction;
    private readonly Stream _inner;

    public SqlFileStreamReadStream(SqlConnection connection, SqlTransaction transaction, Stream inner)
    {
        _connection = connection;
        _transaction = transaction;
        _inner = inner;
    }

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => _inner.CanSeek;

    public override bool CanWrite => false;

    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override void Flush() => _inner.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
        => await _inner.ReadAsync(buffer, cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StorageDisposeHelper.DisposeQuietly(_inner);
            StorageDisposeHelper.DisposeQuietly(_transaction);
            StorageDisposeHelper.DisposeQuietly(_connection);
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await StorageDisposeHelper.DisposeQuietlyAsync(_inner).ConfigureAwait(false);
        await StorageDisposeHelper.DisposeQuietlyAsync(_transaction).ConfigureAwait(false);
        await StorageDisposeHelper.DisposeQuietlyAsync(_connection).ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
}
