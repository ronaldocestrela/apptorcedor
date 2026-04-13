using AppTorcedor.Identity;

namespace AppTorcedor.Application.Abstractions;

public interface IMembershipAdministrationPort
{
    Task<AdminMembershipListPageDto> ListMembershipsAsync(
        MembershipStatus? status,
        Guid? userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminMembershipDetailDto?> GetMembershipByIdAsync(Guid membershipId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MembershipHistoryEventDto>> ListHistoryAsync(
        Guid membershipId,
        int take,
        CancellationToken cancellationToken = default);

    Task<MembershipStatusUpdateResult> UpdateStatusAsync(
        Guid membershipId,
        MembershipStatus status,
        string reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record AdminMembershipListPageDto(int TotalCount, IReadOnlyList<AdminMembershipListItemDto> Items);

public sealed record AdminMembershipListItemDto(
    Guid MembershipId,
    Guid UserId,
    string UserEmail,
    string UserName,
    string Status,
    Guid? PlanId,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    DateTimeOffset? NextDueDate);

public sealed record AdminMembershipDetailDto(
    Guid MembershipId,
    Guid UserId,
    string UserEmail,
    string UserName,
    string Status,
    Guid? PlanId,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    DateTimeOffset? NextDueDate);

public sealed record MembershipHistoryEventDto(
    Guid Id,
    string EventType,
    string? FromStatus,
    string ToStatus,
    Guid? FromPlanId,
    Guid? ToPlanId,
    string Reason,
    Guid? ActorUserId,
    DateTimeOffset CreatedAt);

public sealed record MembershipStatusUpdateResult(bool Ok, MembershipStatusUpdateError? Error);

public enum MembershipStatusUpdateError
{
    NotFound,
    Unchanged,
}
