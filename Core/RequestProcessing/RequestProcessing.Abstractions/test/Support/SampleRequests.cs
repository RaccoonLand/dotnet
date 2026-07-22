using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Support;

internal sealed class SampleRequest : IRequest;

internal sealed class SampleQuery : IQuery<string>;
