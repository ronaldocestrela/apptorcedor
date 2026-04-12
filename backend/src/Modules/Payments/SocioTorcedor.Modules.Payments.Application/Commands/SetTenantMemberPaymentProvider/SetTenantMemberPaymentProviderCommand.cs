using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Commands.SetTenantMemberPaymentProvider;

public sealed record SetTenantMemberPaymentProviderCommand(
    Guid TenantId,
    MemberPaymentProviderKind Provider) : ICommand;
