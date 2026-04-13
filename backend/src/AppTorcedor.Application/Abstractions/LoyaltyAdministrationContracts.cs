namespace AppTorcedor.Application.Abstractions;

public enum LoyaltyCampaignStatus
{
    Draft = 0,
    Published = 1,
    Unpublished = 2,
}

public enum LoyaltyPointRuleTrigger
{
    PaymentPaid = 0,
    TicketPurchased = 1,
    TicketRedeemed = 2,
}

public enum LoyaltyPointSourceType
{
    Payment = 0,
    TicketPurchase = 1,
    TicketRedeem = 2,
    Manual = 3,
}

public sealed record LoyaltyPointRuleWriteDto(LoyaltyPointRuleTrigger Trigger, int Points, int SortOrder);

public sealed record LoyaltyCampaignWriteDto(string Name, string? Description, IReadOnlyList<LoyaltyPointRuleWriteDto> Rules);

public sealed record LoyaltyPointRuleDto(Guid RuleId, LoyaltyPointRuleTrigger Trigger, int Points, int SortOrder);

public sealed record LoyaltyCampaignListItemDto(
    Guid CampaignId,
    string Name,
    LoyaltyCampaignStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    int RuleCount);

public sealed record LoyaltyCampaignListPageDto(int TotalCount, IReadOnlyList<LoyaltyCampaignListItemDto> Items);

public sealed record LoyaltyCampaignDetailDto(
    Guid CampaignId,
    string Name,
    string? Description,
    LoyaltyCampaignStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? UnpublishedAt,
    IReadOnlyList<LoyaltyPointRuleDto> Rules);

public sealed record LoyaltyLedgerEntryDto(
    Guid EntryId,
    Guid UserId,
    Guid? CampaignId,
    Guid? RuleId,
    int Points,
    LoyaltyPointSourceType SourceType,
    string SourceKey,
    string? Reason,
    Guid? ActorUserId,
    DateTimeOffset CreatedAt);

public sealed record LoyaltyLedgerPageDto(int TotalCount, IReadOnlyList<LoyaltyLedgerEntryDto> Items);

public sealed record LoyaltyRankingRowDto(int Rank, Guid UserId, string UserEmail, string UserName, int TotalPoints);

public sealed record LoyaltyRankingPageDto(int TotalCount, IReadOnlyList<LoyaltyRankingRowDto> Items);

public enum LoyaltyMutationError
{
    NotFound,
    Validation,
    InvalidState,
}

public sealed record LoyaltyMutationResult(bool Ok, LoyaltyMutationError? Error)
{
    public static LoyaltyMutationResult Success() => new(true, null);
    public static LoyaltyMutationResult Fail(LoyaltyMutationError error) => new(false, error);
}

public sealed record LoyaltyCampaignCreateResult(Guid? CampaignId, LoyaltyMutationError? Error)
{
    public bool Ok => CampaignId is not null && Error is null;
}

public enum LoyaltyManualAdjustError
{
    NotFound,
    Validation,
}

public sealed record LoyaltyManualAdjustResult(bool Ok, LoyaltyManualAdjustError? Error)
{
    public static LoyaltyManualAdjustResult Success() => new(true, null);
    public static LoyaltyManualAdjustResult Fail(LoyaltyManualAdjustError error) => new(false, error);
}

public interface ILoyaltyAdministrationPort
{
    Task<LoyaltyCampaignListPageDto> ListCampaignsAsync(
        LoyaltyCampaignStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<LoyaltyCampaignDetailDto?> GetCampaignByIdAsync(Guid campaignId, CancellationToken cancellationToken = default);

    Task<LoyaltyCampaignCreateResult> CreateCampaignAsync(LoyaltyCampaignWriteDto dto, CancellationToken cancellationToken = default);

    Task<LoyaltyMutationResult> UpdateCampaignAsync(
        Guid campaignId,
        LoyaltyCampaignWriteDto dto,
        CancellationToken cancellationToken = default);

    Task<LoyaltyMutationResult> PublishCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);

    Task<LoyaltyMutationResult> UnpublishCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);

    Task<LoyaltyManualAdjustResult> ManualAdjustAsync(
        Guid userId,
        int points,
        string reason,
        Guid? campaignId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<LoyaltyLedgerPageDto> ListUserLedgerAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<LoyaltyRankingPageDto> GetMonthlyRankingAsync(
        int year,
        int month,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<LoyaltyRankingPageDto> GetAllTimeRankingAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}

/// <summary>Awards points from domain events (payment/ticket). Implemented by the same service as <see cref="ILoyaltyAdministrationPort"/>.</summary>
public interface ILoyaltyPointsTriggerPort
{
    Task AwardPointsForPaymentPaidAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task AwardPointsForTicketPurchasedAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task AwardPointsForTicketRedeemedAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
