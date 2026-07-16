using CapabilityCentricSample.People.Domain;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CapabilityCentricSample.People.Endpoints.Commands.CreatePerson;

public sealed class CreatePersonCommand : ICommand<int>
{
    public string EmployeeCode { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string NationalCode { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string MobileNumber { get; init; } = string.Empty;
    public DateTime EmploymentDate { get; init; }

    public Stream PhotoContent { get; init; } = Stream.Null;
    public string PhotoContentType { get; init; } = string.Empty;
    public long PhotoContentLength { get; init; }

    public Stream ResumeContent { get; init; } = Stream.Null;
    public string ResumeContentType { get; init; } = string.Empty;
    public long ResumeContentLength { get; init; }
}
