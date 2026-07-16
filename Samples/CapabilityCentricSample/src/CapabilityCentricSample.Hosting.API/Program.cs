using CapabilityCentricSample.Hosting.API;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using RaccoonLand.Modules.Observability.Logging.Serilog.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Allow ~220 MiB multipart uploads for FileStorage streaming diagnostics.
const long diagnosticsMaxBodyBytes = FileStorageDiagnosticsLimits.MaxRequestBodyBytes;
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = diagnosticsMaxBodyBytes;
});
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = diagnosticsMaxBodyBytes;
});

builder.Host.UseRaccoonLandSerilog(builder.Configuration);
builder.Services.AddCapabilityCentricSampleApi(builder.Configuration);

var app = builder.Build();

app.UseCapabilityCentricSampleApi();

app.Run();

internal static class FileStorageDiagnosticsLimits
{
    public const long MaxRequestBodyBytes = 220L * 1024 * 1024;
}
