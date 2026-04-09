using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Queries.ListMembers;

public sealed record ListMembersQuery(int Page, int PageSize) : IQuery<PagedResult<MemberProfileDto>>;
