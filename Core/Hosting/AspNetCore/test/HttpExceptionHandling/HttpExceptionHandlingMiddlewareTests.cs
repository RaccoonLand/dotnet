using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.Hosting.AspNetCore.HttpExceptionHandling;

namespace RaccoonLand.Core.Hosting.AspNetCore.Tests.HttpExceptionHandling;

public sealed class HttpExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_UnhandledException_Writes500ProblemDetails_WithoutLeakingDetails()
    {
        var httpContext = CreateHttpContext("/orders/1");
        RequestDelegate next = _ => throw new InvalidOperationException("secret-db-connection");

        var middleware = CreateMiddleware(next, new HttpExceptionHandlingOptions());

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        var json = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred.", root.GetProperty("title").GetString());
        Assert.Equal("about:blank", root.GetProperty("type").GetString());
        Assert.Equal("/orders/1", root.GetProperty("instance").GetString());
        Assert.DoesNotContain("secret-db-connection", json, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", json, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_UsesProblemJsonContentType()
    {
        // RFC 7807 requires application/problem+json. Clients that route by content-type
        // (Refit, Swagger UI, API gateways) misbehave when the fallback ships as application/json.
        var httpContext = CreateHttpContext();
        RequestDelegate next = _ => throw new InvalidOperationException("x");

        var middleware = CreateMiddleware(next, new HttpExceptionHandlingOptions());

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.NotNull(httpContext.Response.ContentType);
        Assert.StartsWith(
            "application/problem+json",
            httpContext.Response.ContentType!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_ClearsResponseHeaders_SetByDownstream()
    {
        // Regression: an action that starts producing (say) a CSV download and then throws must
        // not leak Content-Disposition / Cache-Control / Set-Cookie into the 500 body. Browsers
        // would otherwise download the JSON error as the CSV, and CDNs would cache the 500.
        var httpContext = CreateHttpContext("/reports/orders.csv");
        RequestDelegate next = ctx =>
        {
            ctx.Response.Headers.ContentDisposition = "attachment; filename=orders.csv";
            ctx.Response.Headers.CacheControl = "public, max-age=300";
            ctx.Response.Headers.ETag = "\"leaked-etag\"";
            ctx.Response.Headers.Append("Set-Cookie", "poison=1; Path=/");
            ctx.Response.ContentType = "text/csv";
            throw new InvalidOperationException("boom");
        };

        var middleware = CreateMiddleware(next, new HttpExceptionHandlingOptions());

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.Headers.ContainsKey("Content-Disposition"));
        Assert.False(httpContext.Response.Headers.ContainsKey("Cache-Control"));
        Assert.False(httpContext.Response.Headers.ContainsKey("ETag"));
        Assert.False(httpContext.Response.Headers.ContainsKey("Set-Cookie"));
        Assert.StartsWith(
            "application/problem+json",
            httpContext.Response.ContentType!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_ClearsResponseBody_PartiallyWrittenByDownstream()
    {
        // If a downstream action buffered a partial CSV row before throwing, that byte prefix
        // must not appear before the JSON error envelope.
        var httpContext = CreateHttpContext();
        RequestDelegate next = async ctx =>
        {
            ctx.Response.ContentType = "text/csv";
            await ctx.Response.WriteAsync("id,name\r\n1,leaked-row\r\n");
            throw new InvalidOperationException("boom");
        };

        var middleware = CreateMiddleware(next, new HttpExceptionHandlingOptions());

        await middleware.InvokeAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("leaked-row", body, StringComparison.Ordinal);
        Assert.DoesNotContain("id,name", body, StringComparison.Ordinal);
        // The body should be valid problem-json.
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(500, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task InvokeAsync_CustomHandlers_TriedInOrder_AndShortCircuit()
    {
        var calls = new List<string>();
        var options = new HttpExceptionHandlingOptions();
        options.On<InvalidOperationException>(async (ctx, _) =>
        {
            calls.Add("first");
            ctx.Response.StatusCode = StatusCodes.Status409Conflict;
            await ctx.Response.WriteAsync("handled-first");
            return true;
        });
        options.On<Exception>(async (ctx, _) =>
        {
            calls.Add("second");
            ctx.Response.StatusCode = StatusCodes.Status418ImATeapot;
            await ctx.Response.WriteAsync("handled-second");
            return true;
        });

        var httpContext = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("x"), options);

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(["first"], calls);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        Assert.Equal("handled-first", await new StreamReader(httpContext.Response.Body).ReadToEndAsync());
    }

    [Fact]
    public async Task InvokeAsync_CustomHandlerReturningFalse_AllowsNextHandler()
    {
        var calls = new List<string>();
        var options = new HttpExceptionHandlingOptions();
        options.On<InvalidOperationException>((_, _) =>
        {
            calls.Add("first");
            return Task.FromResult(false);
        });
        options.On<Exception>(async (ctx, _) =>
        {
            calls.Add("second");
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await ctx.Response.WriteAsync("second");
            return true;
        });

        var httpContext = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("x"), options);

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(["first", "second"], calls);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DerivedException_MatchesBaseHandler()
    {
        var handled = false;
        var options = new HttpExceptionHandlingOptions();
        options.On<Exception>(async (ctx, _) =>
        {
            handled = true;
            ctx.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await Task.CompletedTask;
            return true;
        });

        var httpContext = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new ArgumentNullException("p"), options);

        await middleware.InvokeAsync(httpContext);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseHasStarted_RethrowsWithoutFallbackBody()
    {
        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpResponseFeature>(new AlreadyStartedResponseFeature(httpContext.Response.Body));

        RequestDelegate next = _ => throw new InvalidOperationException("after-start");
        var middleware = CreateMiddleware(next, new HttpExceptionHandlingOptions());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(httpContext));
        Assert.Equal("after-start", ex.Message);
        Assert.True(httpContext.Response.HasStarted);

        httpContext.Response.Body.Position = 0;
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("unexpected error", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("about:blank", body, StringComparison.Ordinal);
    }

    private sealed class AlreadyStartedResponseFeature(Stream body) : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = body;

        public bool HasStarted => true;

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }

    private static HttpExceptionHandlingMiddleware CreateMiddleware(
        RequestDelegate next,
        HttpExceptionHandlingOptions options)
        => new(next, Options.Create(options), NullLogger<HttpExceptionHandlingMiddleware>.Instance);

    private static DefaultHttpContext CreateHttpContext(string path = "/")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
