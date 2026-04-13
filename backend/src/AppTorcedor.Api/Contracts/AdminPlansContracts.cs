using System.ComponentModel.DataAnnotations;

namespace AppTorcedor.Api.Contracts;

public sealed class PlanBenefitRequest
{
    public int SortOrder { get; set; }

    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }
}

public sealed class UpsertPlanRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(64)]
    public string BillingCycle { get; set; } = "Monthly";

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsPublished { get; set; }

    [MaxLength(2000)]
    public string? Summary { get; set; }

    [MaxLength(4000)]
    public string? RulesNotes { get; set; }

    public List<PlanBenefitRequest> Benefits { get; set; } = [];
}
