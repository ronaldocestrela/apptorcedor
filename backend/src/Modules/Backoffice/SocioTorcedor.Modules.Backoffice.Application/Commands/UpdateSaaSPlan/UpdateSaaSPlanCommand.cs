using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Backoffice.Application.Commands.CreateSaaSPlan;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.UpdateSaaSPlan;

public sealed record UpdateSaaSPlanCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal MonthlyPrice,
    decimal? YearlyPrice,
    int MaxMembers,
    IReadOnlyList<SaaSPlanFeatureInput>? Features) : ICommand;
