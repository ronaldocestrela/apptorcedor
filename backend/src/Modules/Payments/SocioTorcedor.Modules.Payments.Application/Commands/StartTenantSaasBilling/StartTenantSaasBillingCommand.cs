using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Payments.Application.Commands.StartTenantSaasBilling;

public sealed record StartTenantSaasBillingCommand(Guid TenantId) : ICommand<Guid>;
