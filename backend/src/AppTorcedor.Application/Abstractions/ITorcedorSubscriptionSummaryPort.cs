namespace AppTorcedor.Application.Abstractions;

/// <summary>Leitura de resumo de assinatura para o torcedor autenticado (D.5).</summary>
public interface ITorcedorSubscriptionSummaryPort
{
    Task<MySubscriptionSummaryDto> GetMySubscriptionSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record MySubscriptionSummaryPlanDto(
    Guid PlanId,
    string Name,
    decimal Price,
    string BillingCycle,
    decimal DiscountPercentage);

public sealed record MySubscriptionSummaryPaymentDto(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string Status,
    string? PaymentMethod,
    DateTimeOffset? PaidAt,
    DateTimeOffset DueDate);

public sealed record MySubscriptionSummaryDigitalCardDto(
    MyDigitalCardViewState State,
    string MembershipStatusLabel,
    string? Message);

/// <summary>Resumo pós-contratação: membership, plano, último pagamento e status da carteirinha (C.3).</summary>
public sealed record MySubscriptionSummaryDto(
    bool HasMembership,
    Guid? MembershipId,
    string? MembershipStatus,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    DateTimeOffset? NextDueDate,
    MySubscriptionSummaryPlanDto? Plan,
    MySubscriptionSummaryPaymentDto? LastPayment,
    MySubscriptionSummaryDigitalCardDto? DigitalCard);
