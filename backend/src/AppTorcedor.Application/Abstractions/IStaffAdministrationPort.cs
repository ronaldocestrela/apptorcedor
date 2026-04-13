namespace AppTorcedor.Application.Abstractions;

public interface IStaffAdministrationPort
{
    Task<StaffInviteCreatedDto> CreateInviteAsync(
        string email,
        string name,
        IReadOnlyList<string> roles,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffInviteRowDto>> ListPendingInvitesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffUserRowDto>> ListStaffUsersAsync(CancellationToken cancellationToken = default);

    Task<bool> SetUserActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);

    Task<bool> ReplaceUserRolesAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default);

    /// <summary>Creates the user, assigns roles from the invite, marks invite consumed. Returns null if invalid.</summary>
    Task<AcceptStaffInviteResultDto?> AcceptInviteAsync(
        string plainToken,
        string password,
        string? nameOverride,
        CancellationToken cancellationToken = default);
}

public sealed record AcceptStaffInviteResultDto(Guid UserId, IReadOnlyList<string> Roles);

public sealed record StaffInviteCreatedDto(Guid Id, string Token, DateTimeOffset ExpiresAt);

public sealed record StaffInviteRowDto(
    Guid Id,
    string Email,
    string Name,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

public sealed record StaffUserRowDto(
    Guid Id,
    string Email,
    string Name,
    bool IsActive,
    IReadOnlyList<string> Roles);
