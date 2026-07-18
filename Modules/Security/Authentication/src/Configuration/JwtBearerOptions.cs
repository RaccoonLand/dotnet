namespace RaccoonLand.Modules.Security.Authentication.Configuration;

/// <summary>
/// Strongly typed JWT Bearer scheme settings. Inherits the standard ASP.NET Core
/// <see cref="Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions"/> so bindable handler
/// properties are available from configuration via <c>IConfiguration.Bind</c>
/// (for example Authority, Audience, TokenValidationParameters).
/// Runtime-only members such as event handlers are typically set in <c>configureOptions</c>.
/// </summary>
public sealed class JwtBearerOptions : Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions;
