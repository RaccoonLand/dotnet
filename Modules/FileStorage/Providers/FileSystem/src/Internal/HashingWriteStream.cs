using System.Security.Cryptography;

namespace RaccoonLand.Modules.FileStorage.FileSystem.Internal;

internal sealed class HashingWriteStream : Stream
{
    private readonly Stream _inner;
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    public HashingWriteStream(Stream inner) => _inner = inner;

    public long BytesWritten { get; private set; }

    public string GetChecksumHex() => Convert.ToHexString(_hash.GetHashAndReset()).ToLowerInvariant();

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

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
