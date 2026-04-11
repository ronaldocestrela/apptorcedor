using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Commands.CreateMemberStripeCheckoutSession;

public sealed record CreateMemberStripeCheckoutSessionCommand(
    Guid MemberPlanId,
    string SuccessUrl,
    string CancelUrl) : ICommand<MemberStripeCheckoutSessionDto>;
