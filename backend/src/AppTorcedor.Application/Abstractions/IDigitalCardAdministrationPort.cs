namespace AppTorcedor.Application.Abstractions;

public interface IDigitalCardAdministrationPort
{
    Task<AdminDigitalCardListPageDto> ListDigitalCardsAsync(
        Guid? userId,
        Guid? membershipId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminDigitalCardIssueCandidatesPageDto> ListDigitalCardIssueCandidatesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminDigitalCardDetailDto?> GetDigitalCardByIdAsync(Guid digitalCardId, CancellationToken cancellationToken = default);

    Task<DigitalCardMutationResult> IssueDigitalCardAsync(Guid membershipId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<DigitalCardMutationResult> RegenerateDigitalCardAsync(
        Guid digitalCardId,
        string? reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<DigitalCardMutationResult> InvalidateDigitalCardAsync(
        Guid digitalCardId,
        string reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record AdminDigitalCardListItemDto(
    Guid DigitalCardId,
    Guid UserId,
    Guid MembershipId,
    int Version,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? InvalidatedAt,
    string UserEmail,
    string MembershipStatus);

public sealed record AdminDigitalCardListPageDto(int TotalCount, IReadOnlyList<AdminDigitalCardListItemDto> Items);

public sealed record AdminDigitalCardIssueCandidateItemDto(
    Guid MembershipId,
    Guid UserId,
    string UserName,
    string UserEmail,
    Guid? PlanId,
    string? PlanName);

public sealed record AdminDigitalCardIssueCandidatesPageDto(int TotalCount, IReadOnlyList<AdminDigitalCardIssueCandidateItemDto> Items);

public sealed record AdminDigitalCardDetailDto(
    Guid DigitalCardId,
    Guid UserId,
    Guid MembershipId,
    int Version,
    string Status,
    string Token,
    DateTimeOffset IssuedAt,
    DateTimeOffset? InvalidatedAt,
    string? InvalidationReason,
    string UserEmail,
    string UserName,
    string MembershipStatus,
    Guid? PlanId,
    string? PlanName,
    string? DocumentMasked,
    IReadOnlyList<string> TemplatePreviewLines);

public sealed record DigitalCardMutationResult(bool Ok, DigitalCardMutationError? Error);

public enum DigitalCardMutationError
{
    NotFound,
    MembershipNotEligible,
    AlreadyHasActiveCard,
    InvalidTransition,
    ReasonRequired,
    ReasonTooLong,
}
