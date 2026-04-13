using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ReplaceStaffUserRoles;

public sealed record ReplaceStaffUserRolesCommand(Guid UserId, IReadOnlyList<string> Roles) : IRequest<bool>;
