using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Membership.Domain.Rules;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Domain.Entities;

public sealed class MemberPlan : AggregateRoot
{
    private MemberPlan()
    {
    }

    public string Nome { get; private set; } = null!;

    public string? Descricao { get; private set; }

    public decimal Preco { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Coleção persistida como JSON (owned types); não é entidade.
    /// </summary>
    public List<Vantagem> Vantagens { get; private set; } = [];

    public static MemberPlan Create(
        string nome,
        string? descricao,
        decimal preco,
        IReadOnlyList<Vantagem>? vantagens,
        Func<bool> nameAlreadyExists)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Name is required.", nameof(nome));

        if (preco < 0)
            throw new ArgumentOutOfRangeException(nameof(preco));

        var rule = new PlanNameMustBeUniqueRule(nameAlreadyExists);
        if (rule.IsBroken())
            throw new BusinessRuleValidationException(rule);

        var now = DateTime.UtcNow;
        var plan = new MemberPlan
        {
            Nome = nome.Trim(),
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
            Preco = preco,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Vantagens = vantagens is { Count: > 0 } ? [..vantagens] : []
        };

        return plan;
    }

    public void Update(string nome, string? descricao, decimal preco, Func<bool> nameConflictWithOtherPlan)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Name is required.", nameof(nome));

        if (preco < 0)
            throw new ArgumentOutOfRangeException(nameof(preco));

        var rule = new PlanNameMustBeUniqueRule(nameConflictWithOtherPlan);
        if (rule.IsBroken())
            throw new BusinessRuleValidationException(rule);

        Nome = nome.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        Preco = preco;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReplaceVantagens(IReadOnlyList<Vantagem>? vantagens)
    {
        Vantagens.Clear();
        if (vantagens is null || vantagens.Count == 0)
        {
            UpdatedAt = DateTime.UtcNow;
            return;
        }

        foreach (var v in vantagens)
            Vantagens.Add(v);

        UpdatedAt = DateTime.UtcNow;
    }

    public void ToggleActive()
    {
        IsActive = !IsActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
