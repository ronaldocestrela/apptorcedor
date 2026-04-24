namespace AppTorcedor.Api.Contracts;

public sealed record RegisterPublicRequest(
    string Name,
    string Email,
    string Password,
    string PhoneNumber,
    IReadOnlyList<Guid> AcceptedLegalDocumentVersionIds);

public sealed record RegistrationLegalRequirementsResponse(
    Guid TermsOfUseVersionId,
    Guid PrivacyPolicyVersionId,
    string TermsTitle,
    string PrivacyTitle);

public sealed record MyProfileResponse(
    string? Document,
    DateOnly? BirthDate,
    string? PhotoUrl,
    string? Address);

public sealed record UpsertMyProfileRequest(
    string? Document,
    DateOnly? BirthDate,
    string? PhotoUrl,
    string? Address);

public sealed record ProfilePhotoUploadResponse(string PhotoUrl);
