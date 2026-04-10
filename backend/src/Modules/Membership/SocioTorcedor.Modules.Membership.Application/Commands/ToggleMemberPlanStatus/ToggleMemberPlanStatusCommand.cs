using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Commands.ToggleMemberPlanStatus;

public sealed record ToggleMemberPlanStatusCommand(Guid PlanId) : ICommand<MemberPlanDto>;
