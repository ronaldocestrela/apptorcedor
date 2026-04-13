namespace AppTorcedor.Api.Contracts;

public sealed record TorcedorNewsFeedItemResponse(
    Guid NewsId,
    string Title,
    string? Summary,
    DateTimeOffset PublishedAt,
    DateTimeOffset UpdatedAt);

public sealed record TorcedorNewsFeedPageResponse(int TotalCount, IReadOnlyList<TorcedorNewsFeedItemResponse> Items);

public sealed record TorcedorNewsDetailResponse(
    Guid NewsId,
    string Title,
    string? Summary,
    string Content,
    DateTimeOffset PublishedAt,
    DateTimeOffset UpdatedAt);

public sealed record TorcedorEligibleBenefitOfferResponse(
    Guid OfferId,
    Guid PartnerId,
    string PartnerName,
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt);

public sealed record TorcedorEligibleBenefitOffersPageResponse(
    int TotalCount,
    IReadOnlyList<TorcedorEligibleBenefitOfferResponse> Items);

public sealed record TorcedorGameListItemResponse(
    Guid GameId,
    string Opponent,
    string Competition,
    DateTimeOffset GameDate,
    DateTimeOffset CreatedAt);

public sealed record TorcedorGameListPageResponse(int TotalCount, IReadOnlyList<TorcedorGameListItemResponse> Items);

public sealed record TorcedorTicketListItemResponse(
    Guid TicketId,
    Guid GameId,
    string Opponent,
    string Competition,
    DateTimeOffset GameDate,
    string Status,
    string? ExternalTicketId,
    string? QrCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RedeemedAt);

public sealed record TorcedorTicketListPageResponse(int TotalCount, IReadOnlyList<TorcedorTicketListItemResponse> Items);

public sealed record TorcedorTicketDetailResponse(
    Guid TicketId,
    Guid GameId,
    string Opponent,
    string Competition,
    DateTimeOffset GameDate,
    string Status,
    string? ExternalTicketId,
    string? QrCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? RedeemedAt);

public sealed record TorcedorLoyaltySummaryResponse(
    int TotalPoints,
    int MonthlyPoints,
    int? MonthlyRank,
    int? AllTimeRank,
    DateTimeOffset AsOfUtc);

public sealed record TorcedorLoyaltyRankingRowResponse(int Rank, Guid UserId, string UserName, int TotalPoints);

public sealed record TorcedorLoyaltyMyStandingResponse(int Rank, Guid UserId, string UserName, int TotalPoints);

public sealed record TorcedorLoyaltyRankingPageResponse(
    int TotalCount,
    IReadOnlyList<TorcedorLoyaltyRankingRowResponse> Items,
    TorcedorLoyaltyMyStandingResponse? Me);
