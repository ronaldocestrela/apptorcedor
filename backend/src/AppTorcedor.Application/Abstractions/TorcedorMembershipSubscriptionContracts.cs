using AppTorcedor.Identity;

namespace AppTorcedor.Application.Abstractions;

/// <summary>Erros de negócio ao iniciar contratação (Parte D.3).</summary>
public enum SubscribeMemberError
{
    PlanNotFoundOrNotAvailable,
    AlreadyActiveSubscription,
    SubscriptionPendingPayment,
    MembershipStatusPreventsSubscribe,
}

/// <summary>Resultado de <see cref="ITorcedorMembershipSubscriptionPort.SubscribeToPlanAsync"/>.</summary>
public sealed record SubscribeMemberResult(
    bool Ok,
    SubscribeMemberError? Error,
    Guid? MembershipId,
    Guid? UserId,
    Guid? PlanId,
    MembershipStatus? Status)
{
    public static SubscribeMemberResult Success(
        Guid membershipId,
        Guid userId,
        Guid planId,
        MembershipStatus status) =>
        new(true, null, membershipId, userId, planId, status);

    public static SubscribeMemberResult Failure(SubscribeMemberError error) =>
        new(false, error, null, null, null, null);
}

/// <summary>Porta de escrita para contratação de plano pelo torcedor (sem acoplamento ao backoffice).</summary>
public interface ITorcedorMembershipSubscriptionPort
{
    Task<SubscribeMemberResult> SubscribeToPlanAsync(
        Guid userId,
        Guid planId,
        CancellationToken cancellationToken = default);
}
