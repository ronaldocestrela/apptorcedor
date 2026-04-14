using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateStaffInvite;

public sealed record CreateStaffInviteCommand(
    string Email,
    string Name,
    IReadOnlyList<string> Roles,
    Guid CreatedByUserId) : IRequest<StaffInviteCreatedDto>;
