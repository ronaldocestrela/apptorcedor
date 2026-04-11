using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Commands.CreateTenantSaasBillingPortalSession;

public sealed record CreateTenantSaasBillingPortalSessionCommand(
    Guid TenantId,
    string ReturnUrl) : ICommand<TenantSaasPortalSessionDto>;
