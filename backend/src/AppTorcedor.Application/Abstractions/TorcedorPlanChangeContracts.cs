using AppTorcedor.Identity;

namespace AppTorcedor.Application.Abstractions;

/// <summary>Prefixo de <c>StatusReason</c> em cobranças de proporcional na troca de plano (D.6), reconhecido no callback de pagamento.</summary>
public static class TorcedorPlanChangePaymentReasons
{
    public const string ProrationPrefix = "Ajuste proporcional — troca de plano (D.6).";
}

public enum ChangePlanError
{
    MembershipNotFound,
    MembershipNotActive,
    MissingBillingCycleContext,
    PlanNotFoundOrNotAvailable,
    SamePlan,
}

public sealed record ChangePlanPlanSnapshotDto(
    Guid PlanId,
    string Name,
    decimal Price,
    string BillingCycle,
    decimal DiscountPercentage);

/// <summary>Resultado de <see cref="ITorcedorPlanChangePort.ChangePlanAsync"/>.</summary>
public sealed record ChangePlanResult(
    bool Ok,
    ChangePlanError? Error,
    Guid? MembershipId,
    MembershipStatus? MembershipStatus,
    ChangePlanPlanSnapshotDto? FromPlan,
    ChangePlanPlanSnapshotDto? ToPlan,
    decimal ProrationAmount,
    Guid? PaymentId,
    string? Currency,
    TorcedorSubscriptionPaymentMethod? PaymentMethod,
    TorcedorSubscriptionCheckoutPixDto? Pix,
    TorcedorSubscriptionCheckoutCardDto? Card)
{
    public static ChangePlanResult Failure(ChangePlanError error) =>
        new(false, error, null, null, null, null, 0, null, null, null, null, null);

    public static ChangePlanResult Success(
        Guid membershipId,
        MembershipStatus membershipStatus,
        ChangePlanPlanSnapshotDto fromPlan,
        ChangePlanPlanSnapshotDto toPlan,
        decimal prorationAmount,
        Guid? paymentId,
        string currency,
        TorcedorSubscriptionPaymentMethod? paymentMethod,
        TorcedorSubscriptionCheckoutPixDto? pix,
        TorcedorSubscriptionCheckoutCardDto? card) =>
        new(
            true,
            null,
            membershipId,
            membershipStatus,
            fromPlan,
            toPlan,
            prorationAmount,
            paymentId,
            currency,
            paymentMethod,
            pix,
            card);
}

/// <summary>Troca de plano do torcedor autenticado com proporcional e nova cobrança quando aplicável (D.6).</summary>
public interface ITorcedorPlanChangePort
{
    Task<ChangePlanResult> ChangePlanAsync(
        Guid userId,
        Guid newPlanId,
        TorcedorSubscriptionPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default);
}
