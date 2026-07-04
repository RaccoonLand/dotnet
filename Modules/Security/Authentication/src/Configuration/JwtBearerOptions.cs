namespace RaccoonLand.Modules.Security.Authentication.Configuration;

/// <summary>
/// Strongly typed JWT Bearer scheme settings. Inherits the standard ASP.NET Core
/// <see cref="Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions"/> so every handler
/// property is available and bindable from configuration (Authority, Audience, TokenValidationParameters, and so on).
/// </summary>
public sealed class JwtBearerOptions : Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions;
