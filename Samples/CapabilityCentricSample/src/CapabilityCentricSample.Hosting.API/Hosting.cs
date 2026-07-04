using CapabilityCentricSample.People.Endpoints.Commands.CreatePerson;
using CapabilityCentricSample.Shared.Localizations;
using CapabilityCentricSample.Shared.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using RaccoonLand.Core.Hosting.AspNetCore.Hosting;
using RaccoonLand.Core.Hosting.AspNetCore.HttpExceptionHandling;
using RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.DependencyInjection;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware;
using RaccoonLand.Modules.Middlewares.FluentValidationMiddleware;
using RaccoonLand.Modules.Middlewares.RequestCachingMiddleware;
using RaccoonLand.Modules.Observability.Instrumentation.Diagnostics;
using RaccoonLand.Modules.Observability.Instrumentation.Telemetry;
using RaccoonLand.Modules.OpenApi;
using RaccoonLand.Modules.OpenApi.Scalar;
using RaccoonLand.Modules.OpenApi.Swagger;

namespace CapabilityCentricSample.Hosting.API;

public static class Hosting
{
    public static IServiceCollection AddCapabilityCentricSampleApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddControllers();
        services.AddRaccoonLandOpenApi(configuration);
        services.AddRaccoonLandAspNetCore(configuration);
        services.AddRaccoonLandHttpExceptionHandling(options =>
        {
            options.On<DbUpdateException>(async (httpContext, _) =>
            {
                var localizer = httpContext.RequestServices.GetService<IMessageLocalization>();
                var message = localizer is null
                    ? "A persistence error occurred while saving changes."
                    : localizer[SharedBusinessMessageTemplates.OPERATION_FAILED];

                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await httpContext.Response.WriteAsJsonAsync(new PipelineResponse
                {
                    StatusHint = StatusCodes.Status500InternalServerError,
                    Errors =
                    [
                        new PipelineMessage(SharedBusinessMessageTemplates.OPERATION_FAILED, message),
                    ],
                });

                return true;
            });
        });
        
        services.AddCapabilityCentricSamplePersistence(configuration);

        services.AddRaccoonLandMessageLocalizationSqlServer(configuration);

        services.AddRaccoonLandExceptionHandling(options =>
        {
            options.On<DbUpdateException>((context, _) =>
            {
                var localizer = context.RequestServices.GetService<IMessageLocalization>();
                var message = localizer is null
                    ? "A persistence error occurred while saving changes."
                    : localizer[SharedBusinessMessageTemplates.OPERATION_FAILED];

                context.Response = new PipelineResponse
                {
                    StatusHint = StatusCodes.Status500InternalServerError,
                    Errors =
                    [
                        new PipelineMessage(SharedBusinessMessageTemplates.OPERATION_FAILED, message),
                    ],
                };

                return Task.FromResult(true);
            });
        });

        services.AddRaccoonLandPipelineInstrumentation(configuration);
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource(RaccoonLandTelemetry.ActivitySourceName)
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics
                .AddMeter(RaccoonLandTelemetry.MeterName)
                .AddConsoleExporter());

        services.AddDistributedMemoryCache();
        services.AddRaccoonLandRequestCaching(configuration);

        services.AddRaccoonLandFluentValidation();
        services.AddValidatorsFromAssemblyContaining<CreatePersonCommand>();

        services.AddRaccoonLandRequestProcessing(
            configureCommandPipeline: pipeline =>
            {
                pipeline.UseMiddleware<PipelineInstrumentationMiddleware>();
                pipeline.UseMiddleware<ExceptionHandlingMiddleware>();
                pipeline.UseMiddleware<FluentValidationMiddleware>();
            },
            configureQueryPipeline: pipeline =>
            {
                pipeline.UseMiddleware<PipelineInstrumentationMiddleware>();
                pipeline.UseMiddleware<ExceptionHandlingMiddleware>();
                pipeline.UseMiddleware<FluentValidationMiddleware>();
                pipeline.UseMiddleware<RequestCachingMiddleware>();
            },
            typeof(CreatePersonCommand).Assembly);

        return services;
    }

    public static WebApplication UseCapabilityCentricSampleApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseHttpsRedirection();
        
        app.UseRaccoonLandHttpExceptionHandling();
        
        app.UseRaccoonLandHttpExecutionContext();
        
        app.UseRaccoonLandSwaggerUI();        
        app.MapControllers();
        app.MapRaccoonLandOpenApi();
        app.MapRaccoonLandScalar();

        return app;
    }
}
