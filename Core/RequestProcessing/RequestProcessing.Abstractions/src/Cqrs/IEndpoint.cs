using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

/// <summary>An endpoint (handler) for a request that produces no payload.</summary>
public interface IEndpoint<TRequest>
    where TRequest : IRequestBase
{
    /// <summary>Handles the <paramref name="request"/> and returns a <see cref="Result"/> (success, or errors/warnings).</summary>
    Task<Result> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>An endpoint (handler) for a request that produces a <typeparamref name="TResponse"/>.</summary>
public interface IEndpoint<TRequest, TResponse>
    where TRequest : IRequestBase
{
    /// <summary>
    /// Handles the <paramref name="request"/> and returns a <see cref="Result{TResponse}"/> carrying either the
    /// payload (with optional warnings) or the errors that prevented success.
    /// </summary>
    Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
}
