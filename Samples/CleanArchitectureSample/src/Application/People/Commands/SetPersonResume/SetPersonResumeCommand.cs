using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CleanArchitectureSample.Application.People.Commands.SetPersonResume;

public sealed class SetPersonResumeCommand : ICommand
{
    public int Id { get; init; }

    public Stream Content { get; init; } = Stream.Null;

    public string ContentType { get; init; } = string.Empty;

    public long ContentLength { get; init; }
}
