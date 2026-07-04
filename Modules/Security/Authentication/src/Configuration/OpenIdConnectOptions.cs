namespace RaccoonLand.Modules.Security.Authentication.Configuration;

/// <summary>
/// Strongly typed OpenID Connect scheme settings. Inherits the standard ASP.NET Core
/// <see cref="Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions"/> so every handler
/// property is available and bindable from configuration (Authority, ClientId, ClientSecret, scopes, and so on).
/// </summary>
public sealed class OpenIdConnectOptions : Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions;
