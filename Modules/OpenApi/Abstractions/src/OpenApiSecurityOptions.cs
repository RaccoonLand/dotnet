namespace RaccoonLand.Modules.OpenApi.Abstractions;

/// <summary>
/// JWT Bearer security settings added to the OpenAPI document so the UI can show an Authorize dialog.
/// This only documents the scheme; it does not configure authentication on the server.
/// </summary>
public sealed class OpenApiSecurityOptions
{
    /// <summary>When <see langword="true"/>, a JWT Bearer security scheme is added to the document.</summary>
    public bool EnableJwtBearer { get; set; }

    /// <summary>Security scheme name registered in the document components (defaults to <c>Bearer</c>).</summary>
    public string SchemeName { get; set; } = "Bearer";

    /// <summary>Bearer token format hint shown in the UI (defaults to <c>JWT</c>).</summary>
    public string BearerFormat { get; set; } = "JWT";

    /// <summary>Human-readable description shown in the UI Authorize dialog.</summary>
    public string Description { get; set; } =
        "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"";

    /// <summary>
    /// When <see langword="true"/> (default), a global security requirement referencing the scheme is added so
    /// every operation requires the token in the UI.
    /// </summary>
    public bool ApplyGlobally { get; set; } = true;
}
