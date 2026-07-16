using System.Net;
using System.Text;
using System.Xml.Linq;
using RaccoonLand.Modules.FileStorage.Abstractions;

namespace RaccoonLand.Modules.FileStorage.S3.Internal;

internal sealed class S3ObjectClient
{
    private readonly HttpClient _httpClient;
    private readonly S3ConnectionSettings _settings;

    public S3ObjectClient(HttpClient httpClient, S3ConnectionSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
        _httpClient.Timeout = settings.RequestTimeout;
    }

    public async Task<S3ObjectMetadata> PutObjectAsync(
        string objectKey,
        Stream content,
        string? contentType,
        IReadOnlyDictionary<string, string>? metadata,
        long? contentLength,
        bool createOnly,
        string storageKey,
        CancellationToken cancellationToken)
    {
        var length = ResolveRequiredContentLength(content, contentLength, "S3 put");
        var requestUri = BuildObjectUri(objectKey);
        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
        {
            Content = CreateSizedStreamContent(content, length),
        };

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        if (createOnly)
        {
            // Atomic create: S3 rejects with 412 when the object already exists.
            request.Headers.TryAddWithoutValidation("If-None-Match", "*");
        }

        AddMetadataHeaders(request, metadata);
        await SignAsync(request, S3SigV4Signer.UnsignedPayload, cancellationToken);

        using var response = await SendAsync(request, cancellationToken, createOnlyAlreadyExistsKey: createOnly ? storageKey : null);
        var etag = NormalizeEtag(response.Headers.ETag?.Tag);

        return new S3ObjectMetadata(objectKey, etag, contentType, length, DateTimeOffset.UtcNow);
    }

    public async Task<(Stream Content, S3ObjectMetadata Metadata)> GetObjectAsync(
        string objectKey,
        CancellationToken cancellationToken)
    {
        var requestUri = BuildObjectUri(objectKey);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        await SignAsync(request, S3SigV4Signer.UnsignedPayload, cancellationToken);

        var response = await SendAsync(request, cancellationToken);
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var metadata = new S3ObjectMetadata(
            objectKey,
            NormalizeEtag(response.Headers.ETag?.Tag),
            response.Content.Headers.ContentType?.MediaType,
            response.Content.Headers.ContentLength,
            DateTimeOffset.UtcNow);

        return (new HttpResponseStream(response, stream), metadata);
    }

    public async Task DeleteObjectAsync(string objectKey, CancellationToken cancellationToken)
    {
        var requestUri = BuildObjectUri(objectKey);
        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
        await SignAsync(request, S3SigV4Signer.UnsignedPayload, cancellationToken);
        using var response = await SendAsync(request, cancellationToken);
    }

    public async Task<bool> ObjectExistsAsync(string objectKey, CancellationToken cancellationToken)
        => await HeadObjectAsync(objectKey, cancellationToken) is not null;

    public async Task<S3ObjectMetadata?> HeadObjectAsync(string objectKey, CancellationToken cancellationToken)
    {
        var requestUri = BuildObjectUri(objectKey);
        using var request = new HttpRequestMessage(HttpMethod.Head, requestUri);
        await SignAsync(request, S3SigV4Signer.UnsignedPayload, cancellationToken);

        HttpResponseMessage response;
        try
        {
            response = await SendAsync(request, cancellationToken);
        }
        catch (FileNotFoundStorageException)
        {
            return null;
        }

        using (response)
        {
            return new S3ObjectMetadata(
                objectKey,
                NormalizeEtag(response.Headers.ETag?.Tag),
                response.Content.Headers.ContentType?.MediaType,
                response.Content.Headers.ContentLength,
                DateTimeOffset.UtcNow);
        }
    }

    public Uri CreatePresignedUrl(string method, string objectKey, string? contentType, TimeSpan expiry)
    {
        var requestUri = BuildObjectUri(objectKey);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            headers["content-type"] = contentType;
        }

        return S3SigV4Signer.CreatePresignedUrl(
            _settings,
            method,
            requestUri,
            headers,
            expiry,
            DateTimeOffset.UtcNow);
    }

    public async Task<string> InitiateMultipartUploadAsync(
        string objectKey,
        string contentType,
        IReadOnlyDictionary<string, string>? metadata,
        CancellationToken cancellationToken)
    {
        var requestUri = BuildObjectUri(objectKey, query: "uploads");
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.TryAddWithoutValidation("Content-Type", contentType);
        AddMetadataHeaders(request, metadata);
        await SignAsync(request, S3SigV4Signer.UnsignedPayload, cancellationToken);

        using var response = await SendAsync(request, cancellationToken);
        var xml = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = XDocument.Parse(xml);
        var uploadId = document.Root?.Element(XName.Get("UploadId", document.Root.Name.NamespaceName))?.Value;

        if (string.IsNullOrWhiteSpace(uploadId))
        {
            throw new FileStorageUnavailableException("S3 did not return an upload id.");
        }

        return uploadId;
    }

    public async Task<string> UploadPartAsync(
        string objectKey,
        string uploadId,
        int partNumber,
        Stream content,
        long? contentLength,
        CancellationToken cancellationToken)
    {
        var length = ResolveRequiredContentLength(content, contentLength, "S3 multipart part upload");
        var requestUri = BuildObjectUri(objectKey, query: $"partNumber={partNumber}&uploadId={Uri.EscapeDataString(uploadId)}");
        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
        {
            Content = CreateSizedStreamContent(content, length),
        };

        await SignAsync(request, S3SigV4Signer.UnsignedPayload, cancellationToken);

        using var response = await SendAsync(request, cancellationToken);
        return NormalizeEtag(response.Headers.ETag?.Tag)
               ?? throw new FileStorageUnavailableException("S3 did not return an ETag for uploaded part.");
    }

    public async Task<S3ObjectMetadata> CompleteMultipartUploadAsync(
        string objectKey,
        string uploadId,
        IReadOnlyList<(int PartNumber, string ETag)> parts,
        CancellationToken cancellationToken)
    {
        var requestUri = BuildObjectUri(objectKey, query: $"uploadId={Uri.EscapeDataString(uploadId)}");
        var xml = new StringBuilder();
        xml.Append("<CompleteMultipartUpload xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\">");
        foreach (var part in parts.OrderBy(x => x.PartNumber))
        {
            var etag = part.ETag.StartsWith('"') ? part.ETag : $"\"{part.ETag}\"";
            xml.Append("<Part><PartNumber>")
                .Append(part.PartNumber)
                .Append("</PartNumber><ETag>")
                .Append(etag)
                .Append("</ETag></Part>");
        }

        xml.Append("</CompleteMultipartUpload>");

        var bodyBytes = Encoding.UTF8.GetBytes(xml.ToString());
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new ByteArrayContent(bodyBytes),
        };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");

        await SignAsync(request, ComputeSha256Hex(bodyBytes), cancellationToken);

        using var response = await SendAsync(request, cancellationToken);
        return new S3ObjectMetadata(objectKey, NormalizeEtag(response.Headers.ETag?.Tag), null, null, DateTimeOffset.UtcNow);
    }

    public async Task AbortMultipartUploadAsync(string objectKey, string uploadId, CancellationToken cancellationToken)
    {
        var requestUri = BuildObjectUri(objectKey, query: $"uploadId={Uri.EscapeDataString(uploadId)}");
        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
        await SignAsync(request, S3SigV4Signer.UnsignedPayload, cancellationToken);
        using var response = await SendAsync(request, cancellationToken);
    }

    private async Task SignAsync(HttpRequestMessage request, string payloadHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var timestamp = DateTimeOffset.UtcNow;
        var headers = request.Headers
            .Where(x => x.Value.Any())
            .ToDictionary(x => x.Key, x => string.Join(',', x.Value), StringComparer.OrdinalIgnoreCase);

        if (request.Content?.Headers.ContentType is { } contentType)
        {
            headers["content-type"] = contentType.ToString();
        }

        var signed = S3SigV4Signer.CreateSignedHeaders(
            _settings,
            request.Method.Method,
            request.RequestUri!,
            headers,
            payloadHash,
            timestamp);

        request.Headers.Remove("Authorization");
        request.Headers.Remove("x-amz-date");
        request.Headers.Remove("x-amz-content-sha256");
        request.Headers.Remove("x-amz-security-token");

        foreach (var (key, value) in signed)
        {
            if (key.Equals("content-type", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            request.Headers.TryAddWithoutValidation(key, value);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        string? createOnlyAlreadyExistsKey = null)
    {
        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new FileStorageUnavailableException("S3 request failed.", ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            response.Dispose();
            throw new FileNotFoundStorageException(request.RequestUri?.AbsolutePath ?? "unknown");
        }

        if (response.StatusCode is HttpStatusCode.Forbidden)
        {
            response.Dispose();
            throw new FileAccessDeniedStorageException(body);
        }

        if (response.StatusCode is HttpStatusCode.PreconditionFailed
            && createOnlyAlreadyExistsKey is not null)
        {
            response.Dispose();
            throw new FileAlreadyExistsStorageException(createOnlyAlreadyExistsKey);
        }

        response.Dispose();
        throw new FileStorageUnavailableException($"S3 returned {(int)response.StatusCode}: {body}");
    }

    private static StreamContent CreateSizedStreamContent(Stream content, long contentLength)
    {
        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentLength = contentLength;
        return streamContent;
    }

    private static long ResolveRequiredContentLength(Stream content, long? declaredLength, string operation)
    {
        if (declaredLength is < 0)
        {
            throw new FileStorageValidationException("Content length cannot be negative.");
        }

        if (declaredLength is long length)
        {
            return length;
        }

        if (content.CanSeek)
        {
            var remaining = content.Length - content.Position;
            if (remaining < 0)
            {
                throw new FileStorageValidationException(
                    $"{operation} cannot proceed because the stream position is beyond the stream length.");
            }

            return remaining;
        }

        throw new FileStorageValidationException(
            $"{operation} requires a known content length. " +
            "Set ContentLength on the request, or provide a seekable stream.");
    }

    private Uri BuildObjectUri(string objectKey, string? query = null)
    {
        var encodedKey = string.Join('/', objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

        if (_settings.ForcePathStyle)
        {
            var builder = new UriBuilder(_settings.ServiceUri)
            {
                Path = CombinePath(_settings.ServiceUri.AbsolutePath, _settings.Bucket, encodedKey),
                Query = query,
            };

            return builder.Uri;
        }

        var virtualBuilder = new UriBuilder(_settings.ServiceUri)
        {
            Path = CombinePath(_settings.ServiceUri.AbsolutePath, encodedKey),
            Query = query,
        };

        return virtualBuilder.Uri;
    }

    private static string CombinePath(params string[] segments)
    {
        var parts = segments
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim('/'))
            .Where(x => x.Length > 0);

        return "/" + string.Join('/', parts);
    }

    private static void AddMetadataHeaders(HttpRequestMessage request, IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return;
        }

        foreach (var (key, value) in metadata)
        {
            request.Headers.TryAddWithoutValidation($"x-amz-meta-{key}", value);
        }
    }

    private static string? NormalizeEtag(string? etag)
        => string.IsNullOrWhiteSpace(etag) ? null : etag.Trim('"');

    private static string ComputeSha256Hex(byte[] content)
        => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)).ToLowerInvariant();

    private sealed class HttpResponseStream : Stream
    {
        private readonly HttpResponseMessage _response;
        private readonly Stream _inner;

        public HttpResponseStream(HttpResponseMessage response, Stream inner)
        {
            _response = response;
            _inner = inner;
        }

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => _inner.CanSeek;

        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position { get => _inner.Position; set => _inner.Position = value; }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _response.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

internal sealed record S3ObjectMetadata(
    string ObjectKey,
    string? ETag,
    string? ContentType,
    long? Length,
    DateTimeOffset Timestamp);
