using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Application.DTOs;

public static class MemberPlanMappings
{
    public static VantagemDto ToDto(this Vantagem v) => new(v.Descricao);

    public static MemberPlanDto ToDto(this MemberPlan plan) =>
        new(
            plan.Id,
            plan.Nome,
            plan.Descricao,
            plan.Preco,
            plan.IsActive,
            plan.Vantagens.Select(v => v.ToDto()).ToList(),
            plan.CreatedAt,
            plan.UpdatedAt);
}
