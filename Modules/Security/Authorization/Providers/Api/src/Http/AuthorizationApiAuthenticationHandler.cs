using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authorization.Api.Configuration;

namespace RaccoonLand.Modules.Security.Authorization.Api.Http;

/// <summary>
/// Attaches service-to-service credentials to each outbound call, based on
/// <see cref="ApiAuthorizationOptions.AuthenticationMode"/>. For advanced schemes (OAuth2 client credentials,
/// end-user token propagation) register your own <see cref="DelegatingHandler"/> on the named client instead.
/// </summary>
internal sealed class AuthorizationApiAuthenticationHandler(IOptions<ApiAuthorizationOptions> options) : DelegatingHandler
{
    private readonly ApiAuthorizationOptions _options = options.Value;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        switch (_options.AuthenticationMode)
        {
            case AuthorizationApiAuthenticationMode.ApiKey:
                if (!string.IsNullOrWhiteSpace(_options.ApiKey)
                    && !request.Headers.Contains(_options.ApiKeyHeaderName))
                {
                    request.Headers.TryAddWithoutValidation(_options.ApiKeyHeaderName, _options.ApiKey);
                }

                break;

            case AuthorizationApiAuthenticationMode.Bearer:
                if (!string.IsNullOrWhiteSpace(_options.BearerToken) && request.Headers.Authorization is null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.BearerToken);
                }

                break;

            case AuthorizationApiAuthenticationMode.None:
            default:
                break;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
