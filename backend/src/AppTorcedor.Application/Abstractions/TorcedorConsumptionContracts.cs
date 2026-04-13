using AppTorcedor.Identity;

namespace AppTorcedor.Application.Abstractions;

public sealed record TorcedorNewsFeedItemDto(
    Guid NewsId,
    string Title,
    string? Summary,
    DateTimeOffset PublishedAt,
    DateTimeOffset UpdatedAt);

public sealed record TorcedorNewsFeedPageDto(int TotalCount, IReadOnlyList<TorcedorNewsFeedItemDto> Items);

public sealed record TorcedorNewsDetailDto(
    Guid NewsId,
    string Title,
    string? Summary,
    string Content,
    DateTimeOffset PublishedAt,
    DateTimeOffset UpdatedAt);

public sealed record TorcedorEligibleBenefitOfferItemDto(
    Guid OfferId,
    Guid PartnerId,
    string PartnerName,
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt);

public sealed record TorcedorEligibleBenefitOffersPageDto(
    int TotalCount,
    IReadOnlyList<TorcedorEligibleBenefitOfferItemDto> Items);

public sealed record TorcedorPublishedPlanBenefitDto(Guid BenefitId, string Title, string? Description);

public sealed record TorcedorPublishedPlanItemDto(
    Guid PlanId,
    string Name,
    decimal Price,
    string BillingCycle,
    decimal DiscountPercentage,
    string? Summary,
    IReadOnlyList<TorcedorPublishedPlanBenefitDto> Benefits);

public sealed record TorcedorPublishedPlansCatalogDto(IReadOnlyList<TorcedorPublishedPlanItemDto> Items);

/// <summary>Read-only port for plan catalog visible to torcedor (Parte D.1): published and active plans.</summary>
public interface ITorcedorPublishedPlansReadPort
{
    Task<TorcedorPublishedPlansCatalogDto> ListPublishedActiveAsync(CancellationToken cancellationToken = default);
}

/// <summary>Read-only port for published news (torcedor feed / detail).</summary>
public interface ITorcedorNewsReadPort
{
    Task<TorcedorNewsFeedPageDto> ListPublishedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TorcedorNewsDetailDto?> GetPublishedByIdAsync(Guid newsId, CancellationToken cancellationToken = default);
}

/// <summary>Read-only port for benefit offers eligible to the authenticated user.</summary>
public interface ITorcedorBenefitsReadPort
{
    Task<TorcedorEligibleBenefitOffersPageDto> ListEligibleForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

/// <summary>Shared eligibility rules (plan/status restrictions) aligned with admin resgate.</summary>
public static class BenefitOfferEligibility
{
    /// <summary>
    /// Returns true if the user's membership satisfies plan/status restrictions.
    /// Empty plan list = no plan restriction; empty status list = no status restriction.
    /// </summary>
    public static bool MatchesPlanAndStatus(
        IReadOnlyList<Guid> planEligibilities,
        IReadOnlyList<MembershipStatus> statusEligibilities,
        MembershipRecordSnapshot? membership)
    {
        if (planEligibilities.Count > 0)
        {
            if (membership?.PlanId is not { } pid || !planEligibilities.Contains(pid))
                return false;
        }

        if (statusEligibilities.Count > 0)
        {
            if (membership is null || !statusEligibilities.Contains(membership.Status))
                return false;
        }

        return true;
    }
}

/// <summary>Minimal membership snapshot for eligibility (no navigation).</summary>
public sealed record MembershipRecordSnapshot(Guid? PlanId, MembershipStatus Status);
