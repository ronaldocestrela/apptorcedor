using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Commands.CreateMemberPlan;

public sealed record CreateMemberPlanCommand(
    string Nome,
    string? Descricao,
    decimal Preco,
    IReadOnlyList<string>? Vantagens) : ICommand<MemberPlanDto>;
