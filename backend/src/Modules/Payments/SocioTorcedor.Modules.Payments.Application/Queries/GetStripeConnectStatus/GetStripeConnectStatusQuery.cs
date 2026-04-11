using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.GetStripeConnectStatus;

public sealed record GetStripeConnectStatusQuery(Guid TenantId) : IQuery<StripeConnectStatusDto>;
