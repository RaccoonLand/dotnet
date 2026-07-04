using CleanArchitectureSample.Hosting.API;
using RaccoonLand.Modules.Observability.Logging.Serilog.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseRaccoonLandSerilog(builder.Configuration);
builder.Services.AddCleanArchitectureSampleApi(builder.Configuration);

var app = builder.Build();

app.UseCleanArchitectureSampleApi();

app.Run();
