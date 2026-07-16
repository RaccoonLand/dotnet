using RaccoonLand.Modules.FileStorage.Abstractions;

namespace RaccoonLand.Modules.FileStorage.SqlServer.FileStream.Internal;

/// <summary>Stops reads once the configured upload limit is exceeded.</summary>
internal sealed class MaxUploadLimitStream : Stream
{
    private readonly Stream _inner;
    private readonly long _maxBytes;
    private long _bytesRead;

    public MaxUploadLimitStream(Stream inner, long maxBytes)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);

        _inner = inner;
        _maxBytes = maxBytes;
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

    public override int Read(byte[] buffer, int offset, int count)
    {
        var toRead = GetAllowedReadCount(count);
        var read = _inner.Read(buffer, offset, toRead);
        TrackRead(read);
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var toRead = GetAllowedReadCount(count);
        var read = await _inner.ReadAsync(buffer.AsMemory(offset, toRead), cancellationToken).ConfigureAwait(false);
        TrackRead(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        var toRead = GetAllowedReadCount(buffer.Length);
        var read = await _inner.ReadAsync(buffer[..toRead], cancellationToken).ConfigureAwait(false);
        TrackRead(read);
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private int GetAllowedReadCount(int requestedCount)
    {
        if (requestedCount <= 0)
        {
            return requestedCount;
        }

        var remaining = _maxBytes - _bytesRead;
        if (remaining <= 0)
        {
            ThrowLimitExceeded();
        }

        return (int)Math.Min(requestedCount, remaining);
    }

    private void TrackRead(int read)
    {
        if (read <= 0)
        {
            return;
        }

        _bytesRead += read;
        if (_bytesRead > _maxBytes)
        {
            ThrowLimitExceeded();
        }
    }

    private static void ThrowLimitExceeded(long maxBytes)
        => throw new FileStorageValidationException($"Upload exceeds the configured limit of {maxBytes} bytes.");

    private void ThrowLimitExceeded() => ThrowLimitExceeded(_maxBytes);
}
