namespace AppTorcedor.Application.Modules.Account;

public sealed record RegistrationLegalRequirementsDto(
    Guid TermsOfUseVersionId,
    Guid PrivacyPolicyVersionId,
    string TermsTitle,
    string PrivacyTitle);

public sealed record RegisterTorcedorRequest(
    string Name,
    string Email,
    string Password,
    string? PhoneNumber,
    IReadOnlyList<Guid> AcceptedLegalDocumentVersionIds);

public sealed record RegisterTorcedorResult(bool Succeeded, Guid? UserId, IReadOnlyList<string> Errors)
{
    public static RegisterTorcedorResult Ok(Guid userId) =>
        new(true, userId, []);

    public static RegisterTorcedorResult Fail(params string[] errors) =>
        new(false, null, errors);

    public static RegisterTorcedorResult Fail(IReadOnlyList<string> errors) =>
        new(false, null, errors);
}

public sealed record MyProfileDto(
    string? Document,
    DateOnly? BirthDate,
    string? PhotoUrl,
    string? Address);

public sealed record MyProfileUpsertDto(
    string? Document,
    DateOnly? BirthDate,
    string? PhotoUrl,
    string? Address);
