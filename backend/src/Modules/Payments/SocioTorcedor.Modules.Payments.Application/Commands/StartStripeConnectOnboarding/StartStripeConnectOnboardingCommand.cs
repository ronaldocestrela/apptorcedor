using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Commands.StartStripeConnectOnboarding;

public sealed record StartStripeConnectOnboardingCommand(
    Guid TenantId,
    string RefreshUrl,
    string ReturnUrl) : ICommand<StripeOnboardingLinkDto>;
