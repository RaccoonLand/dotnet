using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;

namespace RaccoonLand.Core.Hosting.AspNetCore.Tests.HttpExecutionContext;

public sealed class HttpExecutionContextPopulatorTests
{
    [Fact]
    public void Resolve_TenantId_PrefersClaimOverHeader()
    {
        var context = CreateHttpContext(
            claims: [new Claim("tenant_id", "from-claim")],
            headers: [("X-Tenant-Id", "from-header")]);
        var options = new HttpExecutionContextOptions
        {
            TenantIdClaim = "tenant_id",
            TenantIdHeader = "X-Tenant-Id",
            GenerateCorrelationIdWhenMissing = false,
        };

        var resolved = HttpExecutionContextPopulator.Resolve(context, options);

        Assert.Equal("from-claim", resolved.TenantId);
    }

    [Fact]
    public void Resolve_TenantId_FallsBackToHeader_WhenClaimMissingOrBlank()
    {
        var context = CreateHttpContext(
            claims: [new Claim("tenant_id", "  ")],
            headers: [("X-Tenant-Id", "from-header")]);
        var options = new HttpExecutionContextOptions
        {
            TenantIdClaim = "tenant_id",
            TenantIdHeader = "X-Tenant-Id",
            GenerateCorrelationIdWhenMissing = false,
        };

        var resolved = HttpExecutionContextPopulator.Resolve(context, options);

        Assert.Equal("from-header", resolved.TenantId);
    }

    [Fact]
    public void Resolve_TenantId_IsNull_WhenClaimAndHeaderMissing()
    {
        var context = CreateHttpContext();
        var options = new HttpExecutionContextOptions
        {
            TenantIdClaim = "tenant_id",
            TenantIdHeader = "X-Tenant-Id",
            GenerateCorrelationIdWhenMissing = false,
        };

        var resolved = HttpExecutionContextPopulator.Resolve(context, options);

        Assert.Null(resolved.TenantId);
    }

    [Fact]
    public void Resolve_CorrelationId_PrefersHeaderOverActivityTraceId()
    {
        using var activity = StartActivityWithTraceId("11111111111111111111111111111111");
        var context = CreateHttpContext(headers: [("X-Correlation-Id", "from-header")]);
        var options = new HttpExecutionContextOptions
        {
            CorrelationIdHeader = "X-Correlation-Id",
            GenerateCorrelationIdWhenMissing = false,
        };

        var resolved = HttpExecutionContextPopulator.Resolve(context, options);

        Assert.Equal("from-header", resolved.CorrelationId);
    }

    [Fact]
    public void Resolve_CorrelationId_UsesActivityTraceId_WhenHeaderMissing()
    {
        using var activity = StartActivityWithTraceId("22222222222222222222222222222222");
        var context = CreateHttpContext();
        var options = new HttpExecutionContextOptions
        {
            CorrelationIdHeader = "X-Correlation-Id",
            GenerateCorrelationIdWhenMissing = false,
        };

        var resolved = HttpExecutionContextPopulator.Resolve(context, options);

        Assert.Equal("22222222222222222222222222222222", resolved.CorrelationId);
    }

    [Fact]
    public void Resolve_CorrelationId_IgnoresZeroActivityTraceId()
    {
        using var activity = StartActivityWithTraceId("00000000000000000000000000000000");
        var context = CreateHttpContext();
        var options = new HttpExecutionContextOptions
        {
            CorrelationIdHeader = "X-Correlation-Id",
            GenerateCorrelationIdWhenMissing = false,
        };

        var resolved = HttpExecutionContextPopulator.Resolve(context, options);

        Assert.Null(resolved.CorrelationId);
    }

    [Fact]
    public void Resolve_CorrelationId_GeneratesWhenMissing_IfEnabled()
    {
        Activity.Current = null;
        var context = CreateHttpContext();
        var options = new HttpExecutionContextOptions
        {
            CorrelationIdHeader = "X-Correlation-Id",
            GenerateCorrelationIdWhenMissing = true,
        };

        var resolved = HttpExecutionContextPopulator.Resolve(context, options);

        Assert.False(string.IsNullOrWhiteSpace(resolved.CorrelationId));
    }

    [Fact]
    public void Resolve_CorrelationId_RemainsNull_WhenGenerationDisabled()
    {
        Activity.Current = null;
        var context = CreateHttpContext();
        var options = new HttpExecutionContextOptions
        {
            CorrelationIdHeader = "X-Correlation-Id",
            GenerateCorrelationIdWhenMissing = false,
        };

        var resolved = HttpExecutionContextPopulator.Resolve(context, options);

        Assert.Null(resolved.CorrelationId);
    }

    [Fact]
    public void Resolve_Header_FirstValue_ReturnsFirstNonEmptyTrimmed()
    {
        var context = CreateHttpContext(headers: [("X-Tenant-Id", "  a  "), ("X-Tenant-Id", "b")]);
        // StringValues with multiple: use Append
        context.Request.Headers.Remove("X-Tenant-Id");
        context.Request.Headers.Append("X-Tenant-Id", "  first  ");
        context.Request.Headers.Append("X-Tenant-Id", "second");

        var options = new HttpExecutionContextOptions
        {
            TenantIdHeader = "X-Tenant-Id",
            TenantIdHeaderMultiValueMode = MultiValueHeaderMode.FirstValue,
            GenerateCorrelationIdWhenMissing = false,
        };

        var resolved = HttpExecutionContextPopulator.Resolve(context, options);

        Assert.Equal("first", resolved.TenantId);
    }

    [Fact]
    public void Resolve_Header_SingleValueOnly_RequiresExactlyOneNonEmpty()
    {
        var context = CreateHttpContext();
        context.Request.Headers.Append("X-Tenant-Id", "only");
        var options = new HttpExecutionContextOptions
        {
            TenantIdHeader = "X-Tenant-Id",
            TenantIdHeaderMultiValueMode = MultiValueHeaderMode.SingleValueOnly,
            GenerateCorrelationIdWhenMissing = false,
        };

        Assert.Equal("only", HttpExecutionContextPopulator.Resolve(context, options).TenantId);

        context.Request.Headers.Remove("X-Tenant-Id");
        context.Request.Headers.Append("X-Tenant-Id", "a");
        context.Request.Headers.Append("X-Tenant-Id", "b");
        Assert.Null(HttpExecutionContextPopulator.Resolve(context, options).TenantId);
    }

    [Fact]
    public void Resolve_Header_Join_JoinsNonEmptyTrimmedValues()
    {
        var context = CreateHttpContext();
        context.Request.Headers.Append("X-Tenant-Id", "  a ");
        context.Request.Headers.Append("X-Tenant-Id", "b");
        context.Request.Headers.Append("X-Tenant-Id", "  ");

        var options = new HttpExecutionContextOptions
        {
            TenantIdHeader = "X-Tenant-Id",
            TenantIdHeaderMultiValueMode = MultiValueHeaderMode.Join,
            GenerateCorrelationIdWhenMissing = false,
        };

        var resolved = HttpExecutionContextPopulator.Resolve(context, options);

        Assert.Equal("a,b", resolved.TenantId);
    }

    [Fact]
    public void EchoCorrelationId_WritesHeader_WhenEnabledAndMissing()
    {
        var context = CreateHttpContext();
        var options = new HttpExecutionContextOptions
        {
            CorrelationIdHeader = "X-Correlation-Id",
            EchoCorrelationIdInResponse = true,
        };

        HttpExecutionContextPopulator.EchoCorrelationId(context, options, "corr-1");

        Assert.Equal("corr-1", context.Response.Headers["X-Correlation-Id"].ToString());
    }

    [Fact]
    public void EchoCorrelationId_DoesNotOverwriteExistingHeader()
    {
        var context = CreateHttpContext();
        context.Response.Headers["X-Correlation-Id"] = "existing";
        var options = new HttpExecutionContextOptions
        {
            CorrelationIdHeader = "X-Correlation-Id",
            EchoCorrelationIdInResponse = true,
        };

        HttpExecutionContextPopulator.EchoCorrelationId(context, options, "new");

        Assert.Equal("existing", context.Response.Headers["X-Correlation-Id"].ToString());
    }

    [Fact]
    public void EchoCorrelationId_WritesNothing_WhenDisabledOrBlank()
    {
        var context = CreateHttpContext();
        var options = new HttpExecutionContextOptions
        {
            CorrelationIdHeader = "X-Correlation-Id",
            EchoCorrelationIdInResponse = false,
        };

        HttpExecutionContextPopulator.EchoCorrelationId(context, options, "corr");
        Assert.False(context.Response.Headers.ContainsKey("X-Correlation-Id"));

        options.EchoCorrelationIdInResponse = true;
        HttpExecutionContextPopulator.EchoCorrelationId(context, options, "  ");
        Assert.False(context.Response.Headers.ContainsKey("X-Correlation-Id"));

        options.CorrelationIdHeader = " ";
        HttpExecutionContextPopulator.EchoCorrelationId(context, options, "corr");
        Assert.False(context.Response.Headers.ContainsKey(" "));
    }

    private static DefaultHttpContext CreateHttpContext(
        Claim[]? claims = null,
        (string Name, string Value)[]? headers = null)
    {
        var context = new DefaultHttpContext();
        if (claims is { Length: > 0 })
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
        }

        if (headers is not null)
        {
            foreach (var (name, value) in headers)
            {
                context.Request.Headers[name] = value;
            }
        }

        return context;
    }

    private static Activity StartActivityWithTraceId(string traceIdHex)
    {
        var activity = new Activity("test");
        ActivityTraceId traceId;
        if (traceIdHex == "00000000000000000000000000000000")
        {
            // CreateFromString rejects the all-zero id; CreateFromBytes allows it for this contract test.
            traceId = ActivityTraceId.CreateFromBytes(new byte[16]);
        }
        else
        {
            traceId = ActivityTraceId.CreateFromString(traceIdHex.AsSpan());
        }

        activity.SetParentId(traceId, ActivitySpanId.CreateRandom(), ActivityTraceFlags.None);
        activity.Start();
        return activity;
    }
}
