namespace SocioTorcedor.Modules.Payments.Application.DTOs;

public sealed record StripeConnectStatusDto(
    bool IsConfigured,
    string? StripeAccountId,
    int OnboardingStatus,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted);
