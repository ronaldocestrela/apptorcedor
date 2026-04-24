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
    string PhoneNumber,
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

public enum ProfileUpsertError
{
    UserNotFound,
    InvalidDocument,
    DocumentAlreadyInUse,
}

/// <summary>Resultado de criação/atualização de perfil (CPF e unicidade no <c>Document</c>).</summary>
public sealed record ProfileUpsertResult(bool Succeeded, ProfileUpsertError? Error, IReadOnlyList<string> Messages)
{
    public static ProfileUpsertResult Ok() => new(true, null, []);

    public static ProfileUpsertResult NotFound() =>
        new(false, ProfileUpsertError.UserNotFound, ["Usuário não encontrado."]);

    public static ProfileUpsertResult InvalidDocument(string message = "CPF inválido.") =>
        new(false, ProfileUpsertError.InvalidDocument, [message]);

    public static ProfileUpsertResult DocumentAlreadyInUse() =>
        new(false, ProfileUpsertError.DocumentAlreadyInUse, ["Este CPF já está cadastrado."]);
}
