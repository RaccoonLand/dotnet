using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CleanArchitectureSample.Application.People.Commands.SetPersonPhoto;

public sealed class SetPersonPhotoCommand : ICommand
{
    public int Id { get; init; }

    public Stream Content { get; init; } = Stream.Null;

    public string ContentType { get; init; } = string.Empty;

    public long ContentLength { get; init; }
}
