namespace RaccoonLand.Modules.Security.Authentication.Configuration;

/// <summary>
/// Strongly typed OpenID Connect scheme settings. Inherits the standard ASP.NET Core
/// <see cref="Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions"/> so bindable handler
/// properties are available from configuration via <c>IConfiguration.Bind</c>
/// (for example Authority, ClientId, ClientSecret, scopes).
/// Runtime-only members such as event handlers are typically set in <c>configureOptions</c>.
/// </summary>
public sealed class OpenIdConnectOptions : Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions;
