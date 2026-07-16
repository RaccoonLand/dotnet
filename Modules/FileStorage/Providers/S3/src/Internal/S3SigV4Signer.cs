using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace RaccoonLand.Modules.FileStorage.S3.Internal;

internal static class S3SigV4Signer
{
    private const string Algorithm = "AWS4-HMAC-SHA256";
    private const string Service = "s3";
    private const string PayloadHashHeader = "x-amz-content-sha256";
    public const string UnsignedPayload = "UNSIGNED-PAYLOAD";

    public static Dictionary<string, string> CreateSignedHeaders(
        S3ConnectionSettings settings,
        string method,
        Uri requestUri,
        IReadOnlyDictionary<string, string> headers,
        string payloadHash,
        DateTimeOffset timestamp)
    {
        var amzDate = timestamp.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = timestamp.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var signedHeaders = headers
            .Append(new KeyValuePair<string, string>("host", requestUri.Host))
            .Append(new KeyValuePair<string, string>(PayloadHashHeader, payloadHash))
            .Append(new KeyValuePair<string, string>("x-amz-date", amzDate))
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .GroupBy(x => x.Key.ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.Last().Value.Trim(), StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(settings.SessionToken))
        {
            signedHeaders["x-amz-security-token"] = settings.SessionToken!;
        }

        var canonicalHeaders = string.Join('\n', signedHeaders.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Value}")) + '\n';
        var signedHeaderNames = string.Join(';', signedHeaders.Keys.OrderBy(x => x));
        var canonicalQuery = CanonicalizeQuery(requestUri.Query);
        var canonicalRequest = string.Join('\n',
            method.ToUpperInvariant(),
            CanonicalizePath(requestUri.AbsolutePath),
            canonicalQuery,
            canonicalHeaders,
            signedHeaderNames,
            payloadHash);

        var credentialScope = $"{dateStamp}/{settings.Region}/{Service}/aws4_request";
        var stringToSign = string.Join('\n', Algorithm, amzDate, credentialScope, ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))));
        var signature = ToHex(HmacSha256(GetSignatureKey(settings.SecretAccessKey, dateStamp, settings.Region, Service), stringToSign));

        var authorization = $"{Algorithm} Credential={settings.AccessKeyId}/{credentialScope}, SignedHeaders={signedHeaderNames}, Signature={signature}";

        var result = new Dictionary<string, string>(signedHeaders, StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = authorization,
        };

        return result;
    }

    public static Uri CreatePresignedUrl(
        S3ConnectionSettings settings,
        string method,
        Uri requestUri,
        IReadOnlyDictionary<string, string> headers,
        TimeSpan expiry,
        DateTimeOffset timestamp)
    {
        var amzDate = timestamp.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = timestamp.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var credentialScope = $"{dateStamp}/{settings.Region}/{Service}/aws4_request";

        var query = ParseQuery(requestUri.Query);
        query["X-Amz-Algorithm"] = Algorithm;
        query["X-Amz-Credential"] = $"{settings.AccessKeyId}/{credentialScope}";
        query["X-Amz-Date"] = amzDate;
        query["X-Amz-Expires"] = ((int)expiry.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        query["X-Amz-SignedHeaders"] = BuildSignedHeaderNames(headers, requestUri.Host);

        if (!string.IsNullOrWhiteSpace(settings.SessionToken))
        {
            query["X-Amz-Security-Token"] = settings.SessionToken!;
        }

        var signedHeaders = headers
            .Append(new KeyValuePair<string, string>("host", requestUri.Host))
            .GroupBy(x => x.Key.ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.Last().Value.Trim(), StringComparer.Ordinal);

        var canonicalQuery = CanonicalizeQuery(BuildQueryString(query));
        var canonicalHeaders = string.Join('\n', signedHeaders.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Value}")) + '\n';
        var signedHeaderNames = query["X-Amz-SignedHeaders"];
        var canonicalRequest = string.Join('\n',
            method.ToUpperInvariant(),
            CanonicalizePath(requestUri.AbsolutePath),
            canonicalQuery,
            canonicalHeaders,
            signedHeaderNames,
            UnsignedPayload);

        var stringToSign = string.Join('\n', Algorithm, amzDate, credentialScope, ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))));
        var signature = ToHex(HmacSha256(GetSignatureKey(settings.SecretAccessKey, dateStamp, settings.Region, Service), stringToSign));
        query["X-Amz-Signature"] = signature;

        var builder = new UriBuilder(requestUri)
        {
            Query = BuildQueryString(query),
        };

        return builder.Uri;
    }

    private static string BuildSignedHeaderNames(IReadOnlyDictionary<string, string> headers, string host)
    {
        var names = headers.Keys.Select(x => x.ToLowerInvariant()).Append("host").Distinct().OrderBy(x => x);
        return string.Join(';', names);
    }

    private static Dictionary<string, string> ParseQuery(string? query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        var trimmed = query.StartsWith('?') ? query[1..] : query;
        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }

    private static string BuildQueryString(Dictionary<string, string> query)
        => string.Join('&', query.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));

    private static string CanonicalizeQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var parsed = ParseQuery(query);
        return BuildQueryString(parsed);
    }

    private static string CanonicalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "/";
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 ? "/" : "/" + string.Join('/', segments.Select(Uri.EscapeDataString));
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static byte[] GetSignatureKey(string secretKey, string dateStamp, string region, string service)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + secretKey), dateStamp);
        var kRegion = HmacSha256(kDate, region);
        var kService = HmacSha256(kRegion, service);
        return HmacSha256(kService, "aws4_request");
    }

    private static string ToHex(byte[] data) => Convert.ToHexString(data).ToLowerInvariant();
}
