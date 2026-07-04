using CapabilityCentricSample.Hosting.API;
using RaccoonLand.Modules.Observability.Logging.Serilog.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseRaccoonLandSerilog(builder.Configuration);
builder.Services.AddCapabilityCentricSampleApi(builder.Configuration);

var app = builder.Build();

app.UseCapabilityCentricSampleApi();

app.Run();
