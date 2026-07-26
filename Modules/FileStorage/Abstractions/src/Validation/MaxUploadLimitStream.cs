namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>
/// Read wrapper that enforces a maximum number of bytes and throws
/// <see cref="FileStorageValidationException"/> when the inner stream still has more data.
/// Exact-size uploads (inner EOF at the limit) succeed.
/// </summary>
public sealed class MaxUploadLimitStream : Stream
{
    private readonly Stream _inner;
    private readonly long _maxBytes;
    private long _bytesRead;

    /// <param name="inner">Source stream.</param>
    /// <param name="maxBytes">Maximum bytes that may be read before overflow is treated as an error.</param>
    public MaxUploadLimitStream(Stream inner, long maxBytes)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);

        _inner = inner;
        _maxBytes = maxBytes;
    }

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _bytesRead;
        set => throw new NotSupportedException();
    }

    public override void Flush() => _inner.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (count <= 0)
        {
            return 0;
        }

        var remaining = _maxBytes - _bytesRead;
        if (remaining <= 0)
        {
            return ProbeForOverflow(buffer.AsSpan(offset, 1));
        }

        var toRead = (int)Math.Min(count, remaining);
        var read = _inner.Read(buffer, offset, toRead);
        TrackRead(read);
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (count <= 0)
        {
            return 0;
        }

        var remaining = _maxBytes - _bytesRead;
        if (remaining <= 0)
        {
            return await ProbeForOverflowAsync(buffer.AsMemory(offset, 1), cancellationToken).ConfigureAwait(false);
        }

        var toRead = (int)Math.Min(count, remaining);
        var read = await _inner.ReadAsync(buffer.AsMemory(offset, toRead), cancellationToken).ConfigureAwait(false);
        TrackRead(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (buffer.Length == 0)
        {
            return 0;
        }

        var remaining = _maxBytes - _bytesRead;
        if (remaining <= 0)
        {
            return await ProbeForOverflowAsync(buffer[..1], cancellationToken).ConfigureAwait(false);
        }

        var toRead = (int)Math.Min(buffer.Length, remaining);
        var read = await _inner.ReadAsync(buffer[..toRead], cancellationToken).ConfigureAwait(false);
        TrackRead(read);
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private int ProbeForOverflow(Span<byte> probeBuffer)
    {
        // At the byte limit: EOF is success; any further payload means the upload exceeded the cap.
        var probed = _inner.Read(probeBuffer);
        if (probed > 0)
        {
            ThrowLimitExceeded();
        }

        return 0;
    }

    private async ValueTask<int> ProbeForOverflowAsync(Memory<byte> probeBuffer, CancellationToken cancellationToken)
    {
        var probed = await _inner.ReadAsync(probeBuffer, cancellationToken).ConfigureAwait(false);
        if (probed > 0)
        {
            ThrowLimitExceeded();
        }

        return 0;
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

    private void ThrowLimitExceeded()
        => throw new FileStorageValidationException($"Upload exceeds the configured limit of {_maxBytes} bytes.");
}
