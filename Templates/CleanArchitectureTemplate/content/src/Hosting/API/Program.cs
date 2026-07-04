using CleanArchitectureTemplate.Hosting.API;
using RaccoonLand.Modules.Observability.Logging.Serilog.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseRaccoonLandSerilog(builder.Configuration);
builder.Services.AddTemplateAppApi(builder.Configuration);

var app = builder.Build();

app.UseTemplateAppApi();

app.Run();