using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace RaccoonLand.Core.RequestProcessing.Tests.Support;

internal sealed class DoSomethingCommand : ICommand;

internal sealed class GetSomethingQuery : IQuery<string>;

internal sealed class FailSomethingCommand : ICommand;

internal sealed class DoSomethingEndpoint : IEndpoint<DoSomethingCommand>
{
    public Task<Result> ExecuteAsync(DoSomethingCommand request, CancellationToken cancellationToken)
        => Task.FromResult(Result.Success());
}

internal sealed class GetSomethingEndpoint : IEndpoint<GetSomethingQuery, string>
{
    public Task<Result<string>> ExecuteAsync(GetSomethingQuery request, CancellationToken cancellationToken)
        => Task.FromResult(Result<string>.Success("value"));
}

internal sealed class FailSomethingEndpoint : IEndpoint<FailSomethingCommand>
{
    public Task<Result> ExecuteAsync(FailSomethingCommand request, CancellationToken cancellationToken)
        => Task.FromResult(Result.Failure(new PipelineMessage("CMD_FAIL", "command failed")));
}

internal sealed class FailGetQuery : IQuery<string>;

internal sealed class FailGetEndpoint : IEndpoint<FailGetQuery, string>
{
    public Task<Result<string>> ExecuteAsync(FailGetQuery request, CancellationToken cancellationToken)
        => Task.FromResult(Result<string>.Failure(new PipelineMessage("QRY_FAIL", "query failed")));
}

internal sealed class ScopedMarker
{
    public Guid Id { get; } = Guid.CreateVersion7();
}

internal sealed class MarkerAwareCommand : ICommand;

internal sealed class MarkerAwareEndpoint(ScopedMarker marker) : IEndpoint<MarkerAwareCommand>
{
    public ScopedMarker Marker { get; } = marker;

    public Task<Result> ExecuteAsync(MarkerAwareCommand request, CancellationToken cancellationToken)
        => Task.FromResult(Result.Success());
}
