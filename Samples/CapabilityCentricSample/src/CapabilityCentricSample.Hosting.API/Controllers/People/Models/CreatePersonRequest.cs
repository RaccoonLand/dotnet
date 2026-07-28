namespace CapabilityCentricSample.Hosting.API.Controllers.People.Models;

/// <summary>
/// Multipart form shape for creating a person. Files are bound as <c>IFormFile</c> and mapped onto
/// the pipeline <c>CreatePersonCommand</c> in the controller.
/// </summary>
public sealed class CreatePersonRequest
{
    public string EmployeeCode { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string NationalCode { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string MobileNumber { get; init; } = string.Empty;
    public DateTime EmploymentDate { get; init; }
}
