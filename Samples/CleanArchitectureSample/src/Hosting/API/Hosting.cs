using CleanArchitectureSample.Application.People.Commands.CreatePerson;
using CleanArchitectureSample.Application.People.EventHandlers;
using CleanArchitectureSample.Infrastructure.Persistence.Commands.DependencyInjection;
using CleanArchitectureSample.Infrastructure.Persistence.Queries.DependencyInjection;
using CleanArchitectureSample.People.Domain.Events;
using CleanArchitectureSample.Shared.Localizations;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using RaccoonLand.Core.Hosting.AspNetCore.Hosting;
using RaccoonLand.Core.Hosting.AspNetCore.HttpExceptionHandling;
using RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.DependencyInjection;
using RaccoonLand.Modules.FileStorage.AspNetCore;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using RaccoonLand.Modules.Messaging.OutboxRelay;
using RaccoonLand.Modules.Messaging.SqlServer;
using RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware;
using RaccoonLand.Modules.Middlewares.FluentValidationMiddleware;
using RaccoonLand.Modules.Middlewares.RequestCachingMiddleware;
using RaccoonLand.Modules.Observability.Instrumentation.Diagnostics;
using RaccoonLand.Modules.Observability.Instrumentation.Telemetry;
using RaccoonLand.Modules.OpenApi;
using RaccoonLand.Modules.OpenApi.Scalar;
using RaccoonLand.Modules.OpenApi.Swagger;

namespace CleanArchitectureSample.Hosting.API;

public static class Hosting
{
    public static IServiceCollection AddCleanArchitectureSampleApi(
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
            options.AddFileStorageHandlers();
            options.On<DbUpdateException>(async (httpContext, _) =>
            {
                var localizer = httpContext.RequestServices.GetService<IMessageLocalization>();
                var message = localizer is null
                    ? "A persistence error occurred while saving changes."
                    : localizer.Get(SharedBusinessMessageTemplates.OPERATION_FAILED);

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

        services.AddRaccoonLandFileSystemStorage(configuration);
        services.AddCleanArchitectureSampleCommandsPersistence(configuration);
        services.AddCleanArchitectureSampleQueriesPersistence(configuration);

        services.AddRaccoonLandMessageLocalizationSqlServer(configuration);

        services.AddRaccoonLandExceptionHandling(options =>
        {
            options.On<DbUpdateException>((context, _) =>
            {
                var localizer = context.RequestServices.GetService<IMessageLocalization>();
                var message = localizer is null
                    ? "A persistence error occurred while saving changes."
                    : localizer.Get(SharedBusinessMessageTemplates.OPERATION_FAILED);

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

        var commandConnection = configuration.GetConnectionString("CommandConnection")
            ?? throw new InvalidOperationException("Connection string 'CommandConnection' is required for outbox relay.");

        services.AddRaccoonLandOutboxEventStore(
            configuration,
            configureSql: sql => sql.ConnectionString = commandConnection);
        services.AddRaccoonLandDomainEventHandler<PersonCreated, PersonCreatedHandler>();
        services.AddRaccoonLandOutboxRelay(configuration);

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

    public static WebApplication UseCleanArchitectureSampleApi(this WebApplication app)
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
