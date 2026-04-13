using System.ComponentModel.DataAnnotations;
using AppTorcedor.Identity;

namespace AppTorcedor.Api.Contracts;

public sealed class UpsertBenefitPartnerRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = "";

    [MaxLength(2000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class UpsertBenefitOfferRequest
{
    [Required]
    public Guid PartnerId { get; set; }

    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = "";

    [MaxLength(2000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public DateTimeOffset StartAt { get; set; }

    [Required]
    public DateTimeOffset EndAt { get; set; }

    public List<Guid>? EligiblePlanIds { get; set; }

    public List<MembershipStatus>? EligibleMembershipStatuses { get; set; }
}

public sealed class RedeemBenefitOfferRequest
{
    [Required]
    public Guid UserId { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
