using AppTorcedor.Application.Abstractions;

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

public sealed record TorcedorPublishedPlanBenefitResponse(Guid BenefitId, string Title, string? Description);

public sealed record TorcedorPublishedPlanItemResponse(
    Guid PlanId,
    string Name,
    decimal Price,
    string BillingCycle,
    decimal DiscountPercentage,
    string? Summary,
    IReadOnlyList<TorcedorPublishedPlanBenefitResponse> Benefits);

public sealed record TorcedorPublishedPlansCatalogResponse(IReadOnlyList<TorcedorPublishedPlanItemResponse> Items);

public sealed record TorcedorPublishedPlanDetailBenefitResponse(Guid BenefitId, int SortOrder, string Title, string? Description);

public sealed record TorcedorPublishedPlanDetailResponse(
    Guid PlanId,
    string Name,
    decimal Price,
    string BillingCycle,
    decimal DiscountPercentage,
    string? Summary,
    string? RulesNotes,
    IReadOnlyList<TorcedorPublishedPlanDetailBenefitResponse> Benefits);

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

public sealed class TorcedorSubscriptionCheckoutRequest
{
    public Guid PlanId { get; set; }

    public TorcedorSubscriptionPaymentMethod PaymentMethod { get; set; }
}

public sealed record TorcedorSubscriptionCheckoutPixResponse(string QrCodePayload, string? CopyPasteKey);

public sealed record TorcedorSubscriptionCheckoutCardResponse(string CheckoutUrl);

public sealed record TorcedorSubscriptionCheckoutResponse(
    Guid MembershipId,
    Guid PaymentId,
    string PaymentMethod,
    decimal Amount,
    string Currency,
    string MembershipStatus,
    TorcedorSubscriptionCheckoutPixResponse? Pix,
    TorcedorSubscriptionCheckoutCardResponse? Card);

public sealed class TorcedorChangePlanRequest
{
    public Guid PlanId { get; set; }

    public TorcedorSubscriptionPaymentMethod PaymentMethod { get; set; }
}

public sealed record TorcedorChangePlanPlanSnapshotResponse(
    Guid PlanId,
    string Name,
    decimal Price,
    string BillingCycle,
    decimal DiscountPercentage);

public sealed record TorcedorChangePlanResponse(
    Guid MembershipId,
    string MembershipStatus,
    TorcedorChangePlanPlanSnapshotResponse FromPlan,
    TorcedorChangePlanPlanSnapshotResponse ToPlan,
    decimal ProrationAmount,
    Guid? PaymentId,
    string Currency,
    string? PaymentMethod,
    TorcedorSubscriptionCheckoutPixResponse? Pix,
    TorcedorSubscriptionCheckoutCardResponse? Card);

public sealed record TorcedorCancelMembershipResponse(
    Guid MembershipId,
    string MembershipStatus,
    string Mode,
    DateTimeOffset? AccessValidUntilUtc,
    string Message);

public sealed class TorcedorSubscriptionPaymentCallbackRequest
{
    public Guid PaymentId { get; set; }

    public string? Secret { get; set; }
}
