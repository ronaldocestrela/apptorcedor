namespace SocioTorcedor.Modules.Backoffice.Application.DTOs;

public sealed record SaaSPlanDto(
    Guid Id,
    string Name,
    string? Description,
    decimal MonthlyPrice,
    decimal? YearlyPrice,
    int MaxMembers,
    bool IsActive);
