using AppTorcedor.Identity;

namespace AppTorcedor.Application.Abstractions;

public sealed record BenefitPartnerListItemDto(Guid PartnerId, string Name, bool IsActive, DateTimeOffset CreatedAt);

public sealed record BenefitPartnerListPageDto(int TotalCount, IReadOnlyList<BenefitPartnerListItemDto> Items);

public sealed record BenefitPartnerDetailDto(
    Guid PartnerId,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record BenefitPartnerWriteDto(string Name, string? Description, bool IsActive);

public sealed record BenefitOfferListItemDto(
    Guid OfferId,
    Guid PartnerId,
    string PartnerName,
    string Title,
    bool IsActive,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    DateTimeOffset CreatedAt,
    string? BannerUrl);

public sealed record BenefitOfferListPageDto(int TotalCount, IReadOnlyList<BenefitOfferListItemDto> Items);

public sealed record BenefitOfferDetailDto(
    Guid OfferId,
    Guid PartnerId,
    string Title,
    string? Description,
    bool IsActive,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<Guid> EligiblePlanIds,
    IReadOnlyList<MembershipStatus> EligibleMembershipStatuses,
    string? BannerUrl);

public sealed record BenefitOfferWriteDto(
    Guid PartnerId,
    string Title,
    string? Description,
    bool IsActive,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    IReadOnlyList<Guid>? EligiblePlanIds,
    IReadOnlyList<MembershipStatus>? EligibleMembershipStatuses);

public sealed record BenefitRedemptionListItemDto(
    Guid RedemptionId,
    Guid OfferId,
    string OfferTitle,
    Guid UserId,
    string UserEmail,
    Guid? ActorUserId,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record BenefitRedemptionListPageDto(int TotalCount, IReadOnlyList<BenefitRedemptionListItemDto> Items);

public enum BenefitMutationError
{
    NotFound,
    Validation,
    InvalidState,
}

public sealed record BenefitMutationResult(bool Ok, BenefitMutationError? Error)
{
    public static BenefitMutationResult Success() => new(true, null);
    public static BenefitMutationResult Fail(BenefitMutationError error) => new(false, error);
}

public sealed record BenefitCreateResult(Guid? Id, BenefitMutationError? Error)
{
    public bool Ok => Id is not null && Error is null;
}

public sealed record BenefitRedeemResult(bool Ok, Guid? RedemptionId, BenefitMutationError? Error)
{
    public static BenefitRedeemResult Success(Guid redemptionId) => new(true, redemptionId, null);
    public static BenefitRedeemResult Fail(BenefitMutationError error) => new(false, null, error);
}

public sealed record BenefitBannerUploadResult(bool Ok, string? BannerUrl, BenefitMutationError? Error)
{
    public static BenefitBannerUploadResult Success(string bannerUrl) => new(true, bannerUrl, null);
    public static BenefitBannerUploadResult Fail(BenefitMutationError error) => new(false, null, error);
}

public interface IBenefitsAdministrationPort
{
    Task<BenefitPartnerListPageDto> ListPartnersAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<BenefitPartnerDetailDto?> GetPartnerByIdAsync(Guid partnerId, CancellationToken cancellationToken = default);

    Task<BenefitCreateResult> CreatePartnerAsync(BenefitPartnerWriteDto dto, CancellationToken cancellationToken = default);

    Task<BenefitMutationResult> UpdatePartnerAsync(
        Guid partnerId,
        BenefitPartnerWriteDto dto,
        CancellationToken cancellationToken = default);

    Task<BenefitOfferListPageDto> ListOffersAsync(
        Guid? partnerId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<BenefitOfferDetailDto?> GetOfferByIdAsync(Guid offerId, CancellationToken cancellationToken = default);

    Task<BenefitCreateResult> CreateOfferAsync(BenefitOfferWriteDto dto, CancellationToken cancellationToken = default);

    Task<BenefitMutationResult> UpdateOfferAsync(
        Guid offerId,
        BenefitOfferWriteDto dto,
        CancellationToken cancellationToken = default);

    Task<BenefitBannerUploadResult> UploadOfferBannerAsync(
        Guid offerId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<BenefitMutationResult> RemoveOfferBannerAsync(Guid offerId, CancellationToken cancellationToken = default);

    Task<BenefitRedeemResult> RedeemOfferAsync(
        Guid offerId,
        Guid userId,
        string? notes,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<BenefitRedemptionListPageDto> ListRedemptionsAsync(
        Guid? offerId,
        Guid? userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
