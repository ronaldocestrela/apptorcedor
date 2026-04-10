using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.GetTenantSaasBilling;

public sealed record GetTenantSaasBillingQuery(Guid TenantId) : IQuery<TenantSaasBillingSubscriptionDto?>;
