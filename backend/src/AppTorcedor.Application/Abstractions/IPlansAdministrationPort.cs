namespace AppTorcedor.Application.Abstractions;

public interface IPlansAdministrationPort
{
    Task<AdminPlanListPageDto> ListPlansAsync(
        string? search,
        bool? isActive,
        bool? isPublished,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminPlanDetailDto?> GetPlanByIdAsync(Guid planId, CancellationToken cancellationToken = default);

    Task<Guid> CreatePlanAsync(AdminPlanWriteDto dto, CancellationToken cancellationToken = default);

    Task<bool> UpdatePlanAsync(Guid planId, AdminPlanWriteDto dto, CancellationToken cancellationToken = default);
}

public sealed record AdminPlanBenefitInputDto(int SortOrder, string Title, string? Description);

public sealed record AdminPlanWriteDto(
    string Name,
    decimal Price,
    string BillingCycle,
    decimal DiscountPercentage,
    bool IsActive,
    bool IsPublished,
    string? Summary,
    string? RulesNotes,
    IReadOnlyList<AdminPlanBenefitInputDto> Benefits);

public sealed record AdminPlanBenefitDto(Guid Id, int SortOrder, string Title, string? Description);

public sealed record AdminPlanListItemDto(
    Guid PlanId,
    string Name,
    decimal Price,
    string BillingCycle,
    decimal DiscountPercentage,
    bool IsActive,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    int BenefitCount);

public sealed record AdminPlanListPageDto(int TotalCount, IReadOnlyList<AdminPlanListItemDto> Items);

public sealed record AdminPlanDetailDto(
    Guid PlanId,
    string Name,
    decimal Price,
    string BillingCycle,
    decimal DiscountPercentage,
    bool IsActive,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    string? Summary,
    string? RulesNotes,
    IReadOnlyList<AdminPlanBenefitDto> Benefits);
