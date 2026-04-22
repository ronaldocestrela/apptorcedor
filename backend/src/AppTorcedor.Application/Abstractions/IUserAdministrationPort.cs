using AppTorcedor.Application.Modules.Account;

namespace AppTorcedor.Application.Abstractions;

public interface IUserAdministrationPort
{
    Task<AdminUserListPageDto> ListUsersAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetailDto?> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> SetAccountActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Merges non-null fields; use empty string to clear a string field.</summary>
    Task<ProfileUpsertResult> UpsertProfileAsync(Guid userId, AdminUserProfileUpsertDto patch, CancellationToken cancellationToken = default);
}

public sealed record AdminUserListPageDto(int TotalCount, IReadOnlyList<AdminUserListItemDto> Items);

public sealed record AdminUserListItemDto(
    Guid Id,
    string Email,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt,
    bool IsStaff,
    string? MembershipStatus,
    string? Document);

public sealed record AdminUserProfileDto(
    string? Document,
    DateOnly? BirthDate,
    string? PhotoUrl,
    string? Address,
    string? AdministrativeNote);

public sealed record AdminUserMembershipSummaryDto(
    Guid MembershipId,
    string Status,
    Guid? PlanId,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    DateTimeOffset? NextDueDate);

public sealed record AdminUserDetailDto(
    Guid Id,
    string Email,
    string Name,
    string? PhoneNumber,
    bool IsActive,
    DateTimeOffset CreatedAt,
    bool IsStaff,
    IReadOnlyList<string> Roles,
    AdminUserProfileDto? Profile,
    AdminUserMembershipSummaryDto? Membership);

public sealed record AdminUserProfileUpsertDto(
    string? Document,
    DateOnly? BirthDate,
    string? PhotoUrl,
    string? Address,
    string? AdministrativeNote);
