using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Queries.GetMyProfile;

public sealed record GetMyProfileQuery : IQuery<MemberProfileDto>;
