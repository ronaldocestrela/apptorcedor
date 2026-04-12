using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Payments.Application.Commands.DetachTenantSaasPaymentMethod;

public sealed record DetachTenantSaasPaymentMethodCommand(
    Guid TenantId,
    string PaymentMethodId) : ICommand;
