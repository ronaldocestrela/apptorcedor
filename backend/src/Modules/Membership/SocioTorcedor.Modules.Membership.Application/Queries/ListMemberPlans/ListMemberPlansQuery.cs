using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Queries.ListMemberPlans;

public sealed record ListMemberPlansQuery(int Page, int PageSize) : IQuery<PagedResult<MemberPlanDto>>;
