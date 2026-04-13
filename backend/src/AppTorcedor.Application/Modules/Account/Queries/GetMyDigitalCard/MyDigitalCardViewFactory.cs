using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.DigitalCard;
using AppTorcedor.Identity;

namespace AppTorcedor.Application.Modules.Account.Queries.GetMyDigitalCard;

public static class MyDigitalCardViewFactory
{
    public static MyDigitalCardViewDto NoMembershipRow() =>
        new(
            MyDigitalCardViewState.NotAssociated,
            nameof(MembershipStatus.NaoAssociado),
            "Você não possui registro de associação. Torne-se sócio para obter a carteirinha digital.",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

    public static MyDigitalCardViewDto NotAssociated(Guid membershipId) =>
        new(
            MyDigitalCardViewState.NotAssociated,
            nameof(MembershipStatus.NaoAssociado),
            "Você ainda não é sócio associado. Escolha um plano para emitir sua carteirinha digital.",
            membershipId,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

    public static MyDigitalCardViewDto InactiveMembership(MembershipStatus status, Guid membershipId) =>
        new(
            MyDigitalCardViewState.MembershipInactive,
            status.ToString(),
            status switch
            {
                MembershipStatus.Inadimplente =>
                    "Sua associação está inadimplente. Regularize para voltar a acessar a carteirinha digital.",
                MembershipStatus.Suspenso => "Sua associação está suspensa. Entre em contato com o clube.",
                MembershipStatus.Cancelado => "Sua associação foi cancelada.",
                _ => "Não é possível exibir a carteirinha digital no momento.",
            },
            membershipId,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

    public static MyDigitalCardViewDto AwaitingIssuance(Guid membershipId) =>
        new(
            MyDigitalCardViewState.AwaitingIssuance,
            nameof(MembershipStatus.Ativo),
            "Você é sócio ativo, mas ainda não há uma carteirinha emitida. Solicite a emissão ao clube ou aguarde o processamento.",
            membershipId,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

    public static MyDigitalCardViewDto Active(
        Guid membershipId,
        Guid digitalCardId,
        int version,
        DateTimeOffset issuedAt,
        string verificationToken,
        string holderName,
        string membershipStatusLabel,
        string? planName,
        string? documentMasked,
        string cardStatusLabel,
        TimeSpan cacheTtl)
    {
        var lines = DigitalCardTemplatePreview.Build(
            holderName,
            version,
            membershipStatusLabel,
            planName,
            documentMasked,
            cardStatusLabel);
        var until = DateTimeOffset.UtcNow.Add(cacheTtl);
        return new MyDigitalCardViewDto(
            MyDigitalCardViewState.Active,
            nameof(MembershipStatus.Ativo),
            null,
            membershipId,
            digitalCardId,
            version,
            cardStatusLabel,
            issuedAt,
            verificationToken,
            lines,
            until);
    }
}
