using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.DTOs;
using SocioTorcedor.Modules.Membership.Domain.Enums;

namespace SocioTorcedor.Modules.Membership.Application.Queries.ListMembers;

public sealed record ListMembersQuery(int Page, int PageSize, MemberStatus? Status = null)
    : IQuery<PagedResult<MemberProfileDto>>;
