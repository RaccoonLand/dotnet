namespace RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

public interface IRequestBase;

/// <summary>Marker for a request that produces no response.</summary>
public interface IRequest : IRequestBase;

/// <summary>Marker for a request that produces a <typeparamref name="TResponse"/>.</summary>
public interface IRequest<TResponse> : IRequestBase;

/// <summary>A command that changes state and produces no response.</summary>
public interface ICommand : IRequest;

/// <summary>A command that changes state and produces a <typeparamref name="TResponse"/>.</summary>
public interface ICommand<TResponse> : IRequest<TResponse>;

/// <summary>A query that reads state and produces a <typeparamref name="TResponse"/>.</summary>
public interface IQuery<TResponse> : IRequest<TResponse>;
