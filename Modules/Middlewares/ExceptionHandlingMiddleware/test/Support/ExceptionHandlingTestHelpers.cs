using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware.Tests.Support;

internal sealed class SampleRequest : IRequest;

internal static class ExceptionHandlingTestHelpers
{
    public static ExceptionHandlingMiddleware CreateMiddleware(ExceptionHandlingOptions options)
        => new(MsOptions.Create(options));

    public static PipelineContext CreateContext(
        IServiceProvider? services = null,
        CancellationToken cancellationToken = default)
        => new(
            new SampleRequest(),
            services ?? new ServiceCollection().BuildServiceProvider(),
            RequestMetadata.For(typeof(SampleRequest), RequestKind.Command),
            cancellationToken);
}
