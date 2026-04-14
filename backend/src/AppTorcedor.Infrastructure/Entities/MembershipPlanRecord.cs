namespace AppTorcedor.Infrastructure.Entities;

public sealed class MembershipPlanRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string BillingCycle { get; set; } = "Monthly";
    public decimal DiscountPercentage { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Visible no catálogo do torcedor (Parte D). Requer <see cref="IsActive"/>.</summary>
    public bool IsPublished { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Resumo para catálogo (texto curto).</summary>
    public string? Summary { get; set; }

    /// <summary>Notas de regras operacionais básicas (B.5); motor de elegibilidade fica para fases futuras.</summary>
    public string? RulesNotes { get; set; }
}
