namespace AppTorcedor.Infrastructure.Entities;

public enum BenefitShirtCatalogOptionKind
{
    Size = 0,
    Model = 1,
}

/// <summary>Administrative catalog of allowed shirt sizes and models for a customization offer.</summary>
public sealed class BenefitShirtCatalogOptionRecord
{
    public Guid Id { get; set; }
    public Guid OfferId { get; set; }
    public BenefitShirtCatalogOptionKind Kind { get; set; }
    public string Value { get; set; } = "";
    public int SortOrder { get; set; }
}
