using Microsoft.Extensions.Options;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.FileStorage.S3.Configuration;
using RaccoonLand.Modules.FileStorage.S3.Internal;

namespace RaccoonLand.Modules.FileStorage.S3;

internal sealed class S3FileUrlSigner : IFileUrlSigner
{
    private readonly S3ObjectClient _client;
    private readonly S3ConnectionSettings _settings;
    private readonly FileStorageOptions _sharedOptions;

    public S3FileUrlSigner(
        S3ObjectClient client,
        IOptions<S3StorageOptions> options,
        IOptions<FileStorageOptions> sharedOptions)
    {
        _client = client;
        _settings = S3ConnectionSettings.FromOptions(options.Value);
        _sharedOptions = sharedOptions.Value;
    }

    public Task<SignedUrlResult> GetReadUrlAsync(SignedReadUrlRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        var expiry = FileStorageGuards.ResolveExpiry(request.Expiry, _sharedOptions);

        // Do not sign Content-Type on GET. Browsers and typical download clients omit that request header,
        // which would otherwise produce SignatureDoesNotMatch (403). Write URLs still sign Content-Type.
        var url = _client.CreatePresignedUrl("GET", _settings.ToObjectKey(key), contentType: null, expiry);

        return Task.FromResult(new SignedUrlResult(
            url,
            DateTimeOffset.UtcNow.Add(expiry),
            new FileRef { Key = key }));
    }

    public Task<SignedUrlResult> GetWriteUrlAsync(SignedWriteUrlRequest request, CancellationToken cancellationToken = default)
    {
        FileStorageGuards.ValidateSignedWriteRequest(request, _sharedOptions);

        var key = StorageKey.NormalizeOrGenerate(request.Key);
        var expiry = FileStorageGuards.ResolveExpiry(request.Expiry, _sharedOptions);
        var url = _client.CreatePresignedUrl("PUT", _settings.ToObjectKey(key), request.ContentType, expiry);

        return Task.FromResult(new SignedUrlResult(
            url,
            DateTimeOffset.UtcNow.Add(expiry),
            new FileRef { Key = key, ContentType = request.ContentType }));
    }
}
