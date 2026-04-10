namespace SocioTorcedor.Modules.Membership.Application.DTOs;

public sealed record MemberPlanDto(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal Preco,
    bool IsActive,
    IReadOnlyList<VantagemDto> Vantagens,
    DateTime CreatedAt,
    DateTime UpdatedAt);
