namespace RaccoonLand.Modules.OpenApi.Abstractions;

/// <summary>
/// OpenAPI document settings bound from the <c>OpenApi</c> configuration section. Shared by the OpenAPI core
/// (document generation) and the UI provider packages (Swagger UI / Scalar) so the UI knows where the document
/// is served and what to title it.
/// </summary>
public sealed class OpenApiDocumentOptions
{
    /// <summary>Default root configuration section name (<c>OpenApi</c>).</summary>
    public const string SectionName = "OpenApi";

    /// <summary>
    /// When <see langword="false"/>, document generation, the document endpoint, and every UI provider are
    /// skipped (the registration and middleware extensions become no-ops).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>OpenAPI document name (the <c>{documentName}</c> token, for example <c>v1</c>).</summary>
    public string DocumentName { get; set; } = "v1";

    /// <summary>Document title shown in the UI.</summary>
    public string Title { get; set; } = "API";

    /// <summary>OpenAPI document version label (for example <c>v1</c>).</summary>
    public string Version { get; set; } = "v1";

    /// <summary>Optional API description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Route where the generated OpenAPI JSON is served. Supports the <c>{documentName}</c> placeholder.
    /// Defaults to the built-in <c>Microsoft.AspNetCore.OpenApi</c> route.
    /// </summary>
    public string RoutePattern { get; set; } = "/openapi/{documentName}.json";

    /// <summary>JWT Bearer security definition exposed in the document and the UI Authorize dialog.</summary>
    public OpenApiSecurityOptions Security { get; set; } = new();
}
