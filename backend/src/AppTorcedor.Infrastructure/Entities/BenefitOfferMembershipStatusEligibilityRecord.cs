using AppTorcedor.Identity;

namespace AppTorcedor.Infrastructure.Entities;

public sealed class BenefitOfferMembershipStatusEligibilityRecord
{
    public Guid OfferId { get; set; }
    public MembershipStatus Status { get; set; }
}
