using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.GetTenantMemberGatewayStatus;

public sealed record GetTenantMemberGatewayStatusQuery(Guid TenantId) : IQuery<MemberGatewayStatusDto>;
