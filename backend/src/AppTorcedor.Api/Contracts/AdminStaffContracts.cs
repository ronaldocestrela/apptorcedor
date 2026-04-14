namespace AppTorcedor.Api.Contracts;

public sealed record CreateStaffInviteRequest(string Email, string Name, IReadOnlyList<string> Roles);

public sealed record CreateStaffInviteResponse(Guid Id, string Token, DateTimeOffset ExpiresAt);

public sealed record SetStaffUserActiveRequest(bool IsActive);

public sealed record ReplaceStaffUserRolesRequest(IReadOnlyList<string> Roles);
