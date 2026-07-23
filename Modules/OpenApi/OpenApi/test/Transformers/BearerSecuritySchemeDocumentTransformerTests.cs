using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using RaccoonLand.Modules.OpenApi.Abstractions;

namespace RaccoonLand.Modules.OpenApi.Tests.Transformers;

public sealed class BearerSecuritySchemeDocumentTransformerTests
{
    [Fact]
    public async Task TransformAsync_WhenComponentsNull_CreatesComponentsAndAddsScheme()
    {
        var security = new OpenApiSecurityOptions
        {
            SchemeName = "Bearer",
            BearerFormat = "JWT",
            Description = "test-desc",
            ApplyGlobally = false,
        };
        var transformer = new BearerSecuritySchemeDocumentTransformer(security);
        var document = new OpenApiDocument();

        await transformer.TransformAsync(document, context: null!, CancellationToken.None);

        Assert.NotNull(document.Components);
        Assert.NotNull(document.Components.SecuritySchemes);
        var scheme = Assert.IsType<OpenApiSecurityScheme>(
            Assert.Contains("Bearer", (IDictionary<string, IOpenApiSecurityScheme>)document.Components.SecuritySchemes));
        Assert.Equal(SecuritySchemeType.Http, scheme.Type);
        Assert.Equal("bearer", scheme.Scheme);
        Assert.Equal("JWT", scheme.BearerFormat);
        Assert.Equal("test-desc", scheme.Description);
        Assert.Equal(ParameterLocation.Header, scheme.In);
    }

    [Fact]
    public async Task TransformAsync_WithCustomSchemeName_RegistersUnderThatName()
    {
        var transformer = new BearerSecuritySchemeDocumentTransformer(new OpenApiSecurityOptions
        {
            SchemeName = "MyJwt",
            ApplyGlobally = false,
        });
        var document = new OpenApiDocument { Components = new OpenApiComponents() };

        await transformer.TransformAsync(document, context: null!, CancellationToken.None);

        Assert.Contains("MyJwt", document.Components!.SecuritySchemes!);
        Assert.False(document.Components.SecuritySchemes!.ContainsKey("Bearer"));
    }

    [Fact]
    public async Task TransformAsync_WhenSchemeAlreadyExists_Throws()
    {
        var security = new OpenApiSecurityOptions { SchemeName = "Bearer", ApplyGlobally = false };
        var transformer = new BearerSecuritySchemeDocumentTransformer(security);
        var document = new OpenApiDocument
        {
            Components = new OpenApiComponents
            {
                SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer" },
                },
            },
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => transformer.TransformAsync(document, context: null!, CancellationToken.None));

        Assert.Contains("Bearer", ex.Message, StringComparison.Ordinal);
        Assert.Contains("already registered", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TransformAsync_WhenApplyGlobally_AddsSecurityRequirement()
    {
        var transformer = new BearerSecuritySchemeDocumentTransformer(new OpenApiSecurityOptions
        {
            SchemeName = "Bearer",
            ApplyGlobally = true,
        });
        var document = new OpenApiDocument();

        await transformer.TransformAsync(document, context: null!, CancellationToken.None);

        Assert.NotNull(document.Security);
        Assert.NotEmpty(document.Security);
        Assert.Contains("Bearer", document.Components!.SecuritySchemes!);
    }

    [Fact]
    public async Task TransformAsync_WhenApplyGloballyFalse_DoesNotAddSecurityRequirement()
    {
        var transformer = new BearerSecuritySchemeDocumentTransformer(new OpenApiSecurityOptions
        {
            SchemeName = "Bearer",
            ApplyGlobally = false,
        });
        var document = new OpenApiDocument();

        await transformer.TransformAsync(document, context: null!, CancellationToken.None);

        Assert.True(document.Security is null || document.Security.Count == 0);
        Assert.Contains("Bearer", document.Components!.SecuritySchemes!);
    }
}
