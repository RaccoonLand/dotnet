using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Support;

/// <summary>A sample request used to drive the middleware and assert request-name tagging.</summary>
public sealed class SampleCommand : IRequestBase;
