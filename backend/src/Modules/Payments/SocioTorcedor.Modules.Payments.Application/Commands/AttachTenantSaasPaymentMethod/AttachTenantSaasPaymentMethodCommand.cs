using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Payments.Application.Commands.AttachTenantSaasPaymentMethod;

public sealed record AttachTenantSaasPaymentMethodCommand(
    Guid TenantId,
    string PaymentMethodId,
    bool SetAsDefault) : ICommand;
