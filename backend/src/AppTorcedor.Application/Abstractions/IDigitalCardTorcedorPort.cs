using AppTorcedor.Identity;

namespace AppTorcedor.Application.Abstractions;

public interface IDigitalCardTorcedorPort
{
    Task<MyDigitalCardViewDto> GetMyDigitalCardAsync(Guid userId, CancellationToken cancellationToken = default);
}

public enum MyDigitalCardViewState
{
    NotAssociated,
    MembershipInactive,
    AwaitingIssuance,
    Active,
}

/// <summary>Resposta de leitura da carteirinha para o torcedor autenticado (C.3).</summary>
public sealed record MyDigitalCardViewDto(
    MyDigitalCardViewState State,
    string MembershipStatus,
    string? Message,
    Guid? MembershipId,
    Guid? DigitalCardId,
    int? Version,
    string? CardStatus,
    DateTimeOffset? IssuedAt,
    string? VerificationToken,
    IReadOnlyList<string>? TemplatePreviewLines,
    DateTimeOffset? CacheValidUntilUtc);
