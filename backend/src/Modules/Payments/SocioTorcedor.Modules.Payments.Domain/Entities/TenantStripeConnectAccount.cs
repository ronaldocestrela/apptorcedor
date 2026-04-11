using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Domain.Entities;

/// <summary>
/// Conta Stripe Connect Express associada a um tenant (persistido no banco master / Payments master).
/// </summary>
public sealed class TenantStripeConnectAccount : AggregateRoot
{
    private TenantStripeConnectAccount()
    {
    }

    public Guid TenantId { get; private set; }

    public string StripeAccountId { get; private set; } = null!;

    public StripeConnectAccountStatus OnboardingStatus { get; private set; }

    public bool ChargesEnabled { get; private set; }

    public bool PayoutsEnabled { get; private set; }

    public bool DetailsSubmitted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static TenantStripeConnectAccount Create(Guid tenantId, string stripeAccountId)
    {
        if (string.IsNullOrWhiteSpace(stripeAccountId))
            throw new ArgumentException("Stripe account id is required.", nameof(stripeAccountId));

        var now = DateTime.UtcNow;
        return new TenantStripeConnectAccount
        {
            TenantId = tenantId,
            StripeAccountId = stripeAccountId.Trim(),
            OnboardingStatus = StripeConnectAccountStatus.Pending,
            ChargesEnabled = false,
            PayoutsEnabled = false,
            DetailsSubmitted = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void SyncFromStripe(bool chargesEnabled, bool payoutsEnabled, bool detailsSubmitted)
    {
        ChargesEnabled = chargesEnabled;
        PayoutsEnabled = payoutsEnabled;
        DetailsSubmitted = detailsSubmitted;
        OnboardingStatus = chargesEnabled && payoutsEnabled && detailsSubmitted
            ? StripeConnectAccountStatus.Enabled
            : detailsSubmitted
                ? StripeConnectAccountStatus.PendingRequirements
                : StripeConnectAccountStatus.Pending;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
