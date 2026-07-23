using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using RaccoonLand.Modules.Security.Authorization.Abstractions;

namespace RaccoonLand.Modules.Security.Authorization.Middlewares.Tests.Support;

internal sealed record SampleRequest : IRequest;

internal static class AuthorizationMiddlewareTestHelpers
{
    public static PipelineContext CreateContext(
        IAuthorizationProvider provider,
        IMessageLocalization? localizer = null,
        IRequestBase? request = null,
        CancellationToken cancellationToken = default)
    {
        var services = new ServiceCollection();
        services.AddSingleton(provider);

        if (localizer is not null)
        {
            services.AddSingleton(localizer);
        }

        return new PipelineContext(
            request ?? new SampleRequest(),
            RequestKind.Command,
            services.BuildServiceProvider(),
            cancellationToken);
    }
}
