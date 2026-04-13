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
