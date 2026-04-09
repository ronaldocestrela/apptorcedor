using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.CreateSaaSPlan;

public sealed record SaaSPlanFeatureInput(string Key, string? Description, string? Value);

public sealed record CreateSaaSPlanCommand(
    string Name,
    string? Description,
    decimal MonthlyPrice,
    decimal? YearlyPrice,
    int MaxMembers,
    IReadOnlyList<SaaSPlanFeatureInput>? Features) : ICommand<Guid>;
