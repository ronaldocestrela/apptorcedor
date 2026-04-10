using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Commands.UpdateMemberPlan;

public sealed record UpdateMemberPlanCommand(
    Guid PlanId,
    string Nome,
    string? Descricao,
    decimal Preco,
    IReadOnlyList<string>? Vantagens) : ICommand<MemberPlanDto>;
