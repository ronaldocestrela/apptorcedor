using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Queries.GetMemberById;

public sealed record GetMemberByIdQuery(Guid MemberId) : IQuery<MemberProfileDto>;
