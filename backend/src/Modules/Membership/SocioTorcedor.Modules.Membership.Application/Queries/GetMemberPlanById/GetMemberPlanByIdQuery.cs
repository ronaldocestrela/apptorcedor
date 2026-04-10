using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Queries.GetMemberPlanById;

public sealed record GetMemberPlanByIdQuery(Guid PlanId) : IQuery<MemberPlanDto>;
