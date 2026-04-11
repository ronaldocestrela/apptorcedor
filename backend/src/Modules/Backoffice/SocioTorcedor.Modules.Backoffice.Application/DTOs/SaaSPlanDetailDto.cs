namespace SocioTorcedor.Modules.Backoffice.Application.DTOs;

public sealed record SaaSPlanDetailDto(
    Guid Id,
    string Name,
    string? Description,
    decimal MonthlyPrice,
    decimal? YearlyPrice,
    int MaxMembers,
    string? StripePriceMonthlyId,
    string? StripePriceYearlyId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<SaaSPlanFeatureDto> Features);
