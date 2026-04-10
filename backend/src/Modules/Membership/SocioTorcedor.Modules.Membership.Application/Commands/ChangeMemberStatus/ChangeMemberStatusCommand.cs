using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.DTOs;
using SocioTorcedor.Modules.Membership.Domain.Enums;

namespace SocioTorcedor.Modules.Membership.Application.Commands.ChangeMemberStatus;

public sealed record ChangeMemberStatusCommand(Guid MemberId, MemberStatus Status)
    : ICommand<MemberProfileDto>;
