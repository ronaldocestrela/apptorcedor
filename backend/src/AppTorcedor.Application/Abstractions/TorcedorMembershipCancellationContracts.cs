using AppTorcedor.Identity;

namespace AppTorcedor.Application.Abstractions;

public static class TorcedorMembershipCancellationConfigKeys
{
    public const string CoolingOffDays = "Membership.CancellationCoolingOffDays";
}

public static class TorcedorMembershipCancellationDefaults
{
    public const int DefaultCoolingOffDays = 7;
}

public enum CancelMembershipError
{
    MembershipNotFound,
    MembershipAlreadyCancelled,
    CancellationAlreadyScheduled,
    MembershipNotCancellable,
    MissingBillingContext,
}

public enum TorcedorMembershipCancellationMode
{
    /// <summary>Assinatura encerrada imediatamente (ex.: dentro do prazo de arrependimento).</summary>
    Immediate,

    /// <summary>Acesso mantido até <see cref="CancelMembershipResult.AccessValidUntilUtc"/>; status permanece ativo até o sweep efetivar.</summary>
    ScheduledEndOfCycle,
}

/// <summary>Resultado de <see cref="ITorcedorMembershipCancellationPort.CancelMembershipAsync"/> (D.7).</summary>
public sealed record CancelMembershipResult(
    bool Ok,
    CancelMembershipError? Error,
    Guid? MembershipId,
    MembershipStatus? MembershipStatus,
    TorcedorMembershipCancellationMode? Mode,
    DateTimeOffset? AccessValidUntilUtc,
    string? Message)
{
    public static CancelMembershipResult Failure(CancelMembershipError error) =>
        new(false, error, null, null, null, null, null);

    public static CancelMembershipResult Success(
        Guid membershipId,
        MembershipStatus membershipStatus,
        TorcedorMembershipCancellationMode mode,
        DateTimeOffset? accessValidUntilUtc,
        string message) =>
        new(true, null, membershipId, membershipStatus, mode, accessValidUntilUtc, message);
}

/// <summary>Cancelamento de assinatura pelo torcedor autenticado (D.7).</summary>
public interface ITorcedorMembershipCancellationPort
{
    Task<CancelMembershipResult> CancelMembershipAsync(Guid userId, CancellationToken cancellationToken = default);
}
