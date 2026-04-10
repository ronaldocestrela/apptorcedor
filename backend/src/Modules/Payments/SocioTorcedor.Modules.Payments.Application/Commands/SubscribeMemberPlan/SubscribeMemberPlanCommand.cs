using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Commands.SubscribeMemberPlan;

public sealed record SubscribeMemberPlanCommand(Guid MemberPlanId, PaymentMethodKind PaymentMethod)
    : ICommand<Guid>;
