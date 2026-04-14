using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Application.Modules.Administration.Plans;

public static class PlanBillingCycles
{
    public static readonly IReadOnlyList<string> AllowedCanonical =
    [
        "Monthly",
        "Yearly",
        "Quarterly",
    ];

    public static bool TryNormalize(string? input, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmed = input.Trim();
        foreach (var a in AllowedCanonical)
        {
            if (string.Equals(a, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                canonical = a;
                return true;
            }
        }

        return false;
    }
}

public static class PlanWriteValidator
{
    public const int MaxBenefits = 50;

    public static string? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Name is required.";
        if (name.Trim().Length > 256)
            return "Name must be at most 256 characters.";
        return null;
    }

    public static string? ValidatePrice(decimal price) =>
        price < 0 ? "Price cannot be negative." : null;

    public static string? ValidateDiscount(decimal discount) =>
        discount is < 0 or > 100 ? "DiscountPercentage must be between 0 and 100." : null;

    public static string? ValidateBillingCycle(string billingCycle) =>
        PlanBillingCycles.TryNormalize(billingCycle, out _) ? null : "BillingCycle must be Monthly, Yearly or Quarterly.";

    public static string? ValidatePublication(bool isActive, bool isPublished) =>
        isPublished && !isActive ? "Cannot publish an inactive plan." : null;

    public static string? ValidateSummary(string? summary) =>
        summary is { Length: > 2000 } ? "Summary must be at most 2000 characters." : null;

    public static string? ValidateRules(string? rules) =>
        rules is { Length: > 4000 } ? "RulesNotes must be at most 4000 characters." : null;

    public static string? ValidateBenefits(IReadOnlyList<AdminPlanBenefitInputDto> benefits)
    {
        if (benefits.Count > MaxBenefits)
            return $"At most {MaxBenefits} benefits are allowed.";

        foreach (var b in benefits)
        {
            if (string.IsNullOrWhiteSpace(b.Title))
                return "Each benefit requires a non-empty title.";
            if (b.Title.Trim().Length > 256)
                return "Benefit title must be at most 256 characters.";
            if (b.Description is { Length: > 2000 })
                return "Benefit description must be at most 2000 characters.";
        }

        return null;
    }

    public static string? ValidateAll(AdminPlanWriteDto dto)
    {
        return ValidateName(dto.Name)
            ?? ValidatePrice(dto.Price)
            ?? ValidateDiscount(dto.DiscountPercentage)
            ?? ValidateBillingCycle(dto.BillingCycle)
            ?? ValidatePublication(dto.IsActive, dto.IsPublished)
            ?? ValidateSummary(dto.Summary)
            ?? ValidateRules(dto.RulesNotes)
            ?? ValidateBenefits(dto.Benefits);
    }
}
