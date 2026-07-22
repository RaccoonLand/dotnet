using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

namespace RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware.Tests.Support;

internal sealed class SampleRequest : IRequest;

internal static class ExceptionHandlingTestHelpers
{
    public static ExceptionHandlingMiddleware CreateMiddleware(ExceptionHandlingOptions options)
        => new(Options.Create(options));

    public static PipelineContext CreateContext(IServiceProvider? services = null)
        => new(
            new SampleRequest(),
            RequestKind.Command,
            services ?? new ServiceCollection().BuildServiceProvider());
}
