using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.FileStorage.S3.Configuration;
using RaccoonLand.Modules.FileStorage.S3.Internal;

namespace RaccoonLand.Modules.FileStorage.S3;

internal sealed class S3MultipartUploadCoordinator : IMultipartUploadCoordinator
{
    private readonly S3ObjectClient _client;
    private readonly S3ConnectionSettings _settings;
    private readonly FileStorageOptions _sharedOptions;
    private readonly ConcurrentDictionary<string, MultipartUploadBudget> _budgets = new(StringComparer.Ordinal);

    public S3MultipartUploadCoordinator(
        S3ObjectClient client,
        IOptions<S3StorageOptions> options,
        IOptions<FileStorageOptions> sharedOptions)
    {
        _client = client;
        _settings = S3ConnectionSettings.FromOptions(options.Value);
        _sharedOptions = sharedOptions.Value;
    }

    public async Task<MultipartUploadSession> InitiateAsync(
        InitiateMultipartUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        FileStorageGuards.ValidateMultipartInitRequest(request, _sharedOptions);

        var key = StorageKey.NormalizeOrGenerate(request.Key);
        var maxUploadBytes = FileStorageGuards.ResolveEffectiveMaxUploadBytes(
            request.MaxUploadBytes,
            _sharedOptions.MaxUploadBytes);

        var uploadId = await _client.InitiateMultipartUploadAsync(
            _settings.ToObjectKey(key),
            request.ContentType,
            request.Metadata,
            cancellationToken);

        _budgets[uploadId] = new MultipartUploadBudget(maxUploadBytes);

        return new MultipartUploadSession(key, uploadId);
    }

    public async Task<UploadPartResult> UploadPartAsync(
        UploadPartRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PartNumber <= 0)
        {
            throw new FileStorageValidationException("Part number must be greater than zero.");
        }

        if (!_budgets.TryGetValue(request.UploadId, out var budget))
        {
            // Session may have been started in another process; still enforce global max on this part.
            budget = new MultipartUploadBudget(_sharedOptions.MaxUploadBytes);
        }

        var length = ResolvePartLength(request);
        FileStorageGuards.EnsureContentLengthWithinLimit(length, budget.MaxUploadBytes);
        budget.AddBytesOrThrow(length);

        try
        {
            var etag = await _client.UploadPartAsync(
                _settings.ToObjectKey(StorageKey.Normalize(request.Key)),
                request.UploadId,
                request.PartNumber,
                request.Content,
                length,
                budget.MaxUploadBytes,
                cancellationToken);

            return new UploadPartResult(request.PartNumber, etag);
        }
        catch
        {
            budget.AddBytes(-length);
            throw;
        }
    }

    public async Task<FileRef> CompleteAsync(
        CompleteMultipartUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var metadata = await _client.CompleteMultipartUploadAsync(
            _settings.ToObjectKey(StorageKey.Normalize(request.Key)),
            request.UploadId,
            request.Parts.Select(x => (x.PartNumber, x.ETag)).ToList(),
            cancellationToken);

        _budgets.TryRemove(request.UploadId, out _);

        // ContentType/Length are not returned by S3 Complete; MIME is locked at Initiate.
        // Callers should use app session state from Initiate or GetMetadataAsync.
        return new FileRef
        {
            Key = StorageKey.Normalize(request.Key),
            Version = metadata.ETag,
            CreatedAtUtc = metadata.Timestamp,
        };
    }

    public async Task AbortAsync(AbortMultipartUploadRequest request, CancellationToken cancellationToken = default)
    {
        await _client.AbortMultipartUploadAsync(
            _settings.ToObjectKey(StorageKey.Normalize(request.Key)),
            request.UploadId,
            cancellationToken);

        _budgets.TryRemove(request.UploadId, out _);
    }

    private static long ResolvePartLength(UploadPartRequest request)
    {
        if (request.ContentLength is long declared)
        {
            if (declared < 0)
            {
                throw new FileStorageValidationException("Content length cannot be negative.");
            }

            return declared;
        }

        if (request.Content.CanSeek)
        {
            var remaining = request.Content.Length - request.Content.Position;
            if (remaining < 0)
            {
                throw new FileStorageValidationException(
                    "S3 multipart part upload cannot proceed because the stream position is beyond the stream length.");
            }

            return remaining;
        }

        throw new FileStorageValidationException(
            "S3 multipart part upload requires a known content length. " +
            "Set ContentLength on the request, or provide a seekable stream.");
    }

    private sealed class MultipartUploadBudget(long? maxUploadBytes)
    {
        private long _uploadedBytes;

        public long? MaxUploadBytes { get; } = maxUploadBytes;

        public void AddBytesOrThrow(long length)
        {
            while (true)
            {
                var current = Interlocked.Read(ref _uploadedBytes);
                var next = current + length;
                if (MaxUploadBytes is long maxBytes && next > maxBytes)
                {
                    throw new FileStorageValidationException(
                        $"Multipart upload exceeds the configured limit of {maxBytes} bytes.");
                }

                if (Interlocked.CompareExchange(ref _uploadedBytes, next, current) == current)
                {
                    return;
                }
            }
        }

        public void AddBytes(long length) => Interlocked.Add(ref _uploadedBytes, length);
    }
}
