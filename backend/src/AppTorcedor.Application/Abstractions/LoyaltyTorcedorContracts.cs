namespace AppTorcedor.Application.Abstractions;

/// <summary>Torcedor-facing loyalty read model (JWT; no admin permissions).</summary>
public sealed record LoyaltyTorcedorSummaryDto(
    int TotalPoints,
    int MonthlyPoints,
    int? MonthlyRank,
    int? AllTimeRank,
    DateTimeOffset AsOfUtc);

public sealed record LoyaltyTorcedorRankingRowDto(int Rank, Guid UserId, string UserName, int TotalPoints);

public sealed record LoyaltyTorcedorMyStandingDto(int Rank, Guid UserId, string UserName, int TotalPoints);

public sealed record LoyaltyTorcedorRankingPageDto(
    int TotalCount,
    IReadOnlyList<LoyaltyTorcedorRankingRowDto> Items,
    LoyaltyTorcedorMyStandingDto? Me);

public interface ILoyaltyTorcedorReadPort
{
    Task<LoyaltyTorcedorSummaryDto> GetMySummaryAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<LoyaltyTorcedorRankingPageDto> GetMonthlyRankingAsync(
        Guid currentUserId,
        int year,
        int month,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<LoyaltyTorcedorRankingPageDto> GetAllTimeRankingAsync(
        Guid currentUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
