using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Stripe;
using Stripe.Checkout;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class StripeWebhookProcessorTests
{
    [Fact]
    public async Task ProcessVerifiedEventAsync_is_idempotent_by_event_id()
    {
        await using var db = await CreateDbAsync();
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        db.Users.Add(
            new ApplicationUser
            {
                Id = userId,
                Email = "t@test.local",
                NormalizedEmail = "T@TEST.LOCAL",
                UserName = "t@test.local",
                NormalizedUserName = "T@TEST.LOCAL",
                Name = "T",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        db.MembershipPlans.Add(
            new MembershipPlanRecord
            {
                Id = planId,
                Name = "P",
                Price = 90m,
                BillingCycle = "Monthly",
                DiscountPercentage = 0,
                IsActive = true,
                IsPublished = true,
            });
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = membershipId,
                UserId = userId,
                PlanId = planId,
                Status = MembershipStatus.PendingPayment,
                StartDate = DateTimeOffset.UtcNow,
            });
        db.Payments.Add(
            new PaymentRecord
            {
                Id = paymentId,
                UserId = userId,
                MembershipId = membershipId,
                Amount = 90m,
                Status = PaymentChargeStatuses.Pending,
                DueDate = DateTimeOffset.UtcNow.AddDays(1),
                PaymentMethod = "Card",
                ExternalReference = "cs_test_1",
                ProviderName = "Stripe",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        var checkout = new RecordingCheckoutPort();
        var opts = Microsoft.Extensions.Options.Options.Create(new Infrastructure.Options.PaymentsOptions());
        var sut = new StripeWebhookProcessor(db, checkout, opts, NullLogger<StripeWebhookProcessor>.Instance);
        var stripeEvent = BuildCheckoutSessionCompletedEvent(paymentId, amountTotalCents: 9000, eventId: "evt_idem_1");

        var r1 = await sut.ProcessVerifiedEventAsync(stripeEvent, CancellationToken.None);
        var r2 = await sut.ProcessVerifiedEventAsync(stripeEvent, CancellationToken.None);

        Assert.Equal(StripeWebhookProcessResult.Ok, r1);
        Assert.Equal(StripeWebhookProcessResult.Ok, r2);
        Assert.Equal(1, checkout.ConfirmAfterCalls);
        Assert.True(await db.ProcessedStripeWebhookEvents.AnyAsync(e => e.EventId == "evt_idem_1"));
    }

    [Fact]
    public async Task ProcessVerifiedEventAsync_rejects_amount_mismatch()
    {
        await using var db = await CreateDbAsync();
        var paymentId = Guid.NewGuid();
        SeedPayment(db, paymentId, amount: 90m);
        await db.SaveChangesAsync();

        var checkout = new RecordingCheckoutPort();
        var sut = new StripeWebhookProcessor(
            db,
            checkout,
            Microsoft.Extensions.Options.Options.Create(new Infrastructure.Options.PaymentsOptions()),
            NullLogger<StripeWebhookProcessor>.Instance);
        var stripeEvent = BuildCheckoutSessionCompletedEvent(paymentId, amountTotalCents: 8000, eventId: "evt_bad_amt");

        var r = await sut.ProcessVerifiedEventAsync(stripeEvent, CancellationToken.None);
        Assert.Equal(StripeWebhookProcessResult.InvalidPayload, r);
        Assert.Equal(0, checkout.ConfirmAfterCalls);
    }

    private static async Task<AppDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static void SeedPayment(AppDbContext db, Guid paymentId, decimal amount)
    {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        db.Users.Add(
            new ApplicationUser
            {
                Id = userId,
                Email = "t2@test.local",
                NormalizedEmail = "T2@TEST.LOCAL",
                UserName = "t2@test.local",
                NormalizedUserName = "T2@TEST.LOCAL",
                Name = "T2",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        db.MembershipPlans.Add(
            new MembershipPlanRecord
            {
                Id = planId,
                Name = "P2",
                Price = amount,
                BillingCycle = "Monthly",
                DiscountPercentage = 0,
                IsActive = true,
                IsPublished = true,
            });
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = membershipId,
                UserId = userId,
                PlanId = planId,
                Status = MembershipStatus.PendingPayment,
                StartDate = DateTimeOffset.UtcNow,
            });
        db.Payments.Add(
            new PaymentRecord
            {
                Id = paymentId,
                UserId = userId,
                MembershipId = membershipId,
                Amount = amount,
                Status = PaymentChargeStatuses.Pending,
                DueDate = DateTimeOffset.UtcNow.AddDays(1),
                PaymentMethod = "Card",
                ExternalReference = "cs_x",
                ProviderName = "Stripe",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
    }

    private static Event BuildCheckoutSessionCompletedEvent(Guid paymentId, long amountTotalCents, string eventId) =>
        new()
        {
            Id = eventId,
            Type = EventTypes.CheckoutSessionCompleted,
            Data = new EventData
            {
                Object = new Session
                {
                    PaymentStatus = "paid",
                    AmountTotal = amountTotalCents,
                    Currency = "brl",
                    Metadata = new Dictionary<string, string> { ["payment_id"] = paymentId.ToString("D") },
                    PaymentIntentId = "pi_test_webhook",
                },
            },
        };

    [Fact]
    public async Task ProcessVerifiedEventAsync_benefit_freight_paid_approves_redemption()
    {
        await using var db = await CreateDbAsync();
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Users.Add(
            new ApplicationUser
            {
                Id = userId,
                Email = "ship@test.local",
                NormalizedEmail = "SHIP@TEST.LOCAL",
                UserName = "ship@test.local",
                NormalizedUserName = "SHIP@TEST.LOCAL",
                Name = "S",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = now,
            });
        db.BenefitPartners.Add(
            new BenefitPartnerRecord
            {
                Id = partnerId,
                Name = "P",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        db.BenefitOffers.Add(
            new BenefitOfferRecord
            {
                Id = offerId,
                PartnerId = partnerId,
                Title = "Camisa",
                IsActive = true,
                StartAt = now.AddDays(-1),
                EndAt = now.AddDays(30),
                CreatedAt = now,
                UpdatedAt = now,
                IsShirtCustomizationOffer = true,
            });
        db.Payments.Add(
            new PaymentRecord
            {
                Id = paymentId,
                UserId = userId,
                MembershipId = null,
                Amount = 12.68m,
                Status = PaymentChargeStatuses.Pending,
                DueDate = now.AddDays(1),
                PaymentMethod = "Card",
                ExternalReference = "cs_test_ship",
                ProviderName = "Stripe",
                CreatedAt = now,
                UpdatedAt = now,
            });
        db.BenefitRedemptions.Add(
            new BenefitRedemptionRecord
            {
                Id = Guid.NewGuid(),
                OfferId = offerId,
                UserId = userId,
                CreatedAt = now,
                Status = BenefitRedemptionStatus.Pending,
                ShirtSize = "M",
                ShirtModel = "Home",
                ShirtNumber = "10",
                ShirtDisplayName = "T",
                ShippingMethod = "carrier",
                ShippingPrice = 12.68m,
                ShippingPaymentId = paymentId,
            });
        await db.SaveChangesAsync();

        var checkout = new RecordingCheckoutPort();
        var opts = Microsoft.Extensions.Options.Options.Create(new Infrastructure.Options.PaymentsOptions());
        var sut = new StripeWebhookProcessor(db, checkout, opts, NullLogger<StripeWebhookProcessor>.Instance);
        var stripeEvent = BuildCheckoutSessionCompletedEvent(paymentId, amountTotalCents: 1268, eventId: "evt_ship_1");

        var r = await sut.ProcessVerifiedEventAsync(stripeEvent, CancellationToken.None);

        Assert.Equal(StripeWebhookProcessResult.Ok, r);
        Assert.Equal(0, checkout.ConfirmAfterCalls);

        var pay = await db.Payments.AsNoTracking().SingleAsync(p => p.Id == paymentId);
        Assert.Equal(PaymentChargeStatuses.Paid, pay.Status);

        var red = await db.BenefitRedemptions.AsNoTracking().SingleAsync(x => x.ShippingPaymentId == paymentId);
        Assert.Equal(BenefitRedemptionStatus.Approved, red.Status);
        Assert.NotNull(red.ShippingPaidAtUtc);
    }

    private sealed class RecordingCheckoutPort : ITorcedorSubscriptionCheckoutPort
    {
        public int ConfirmAfterCalls { get; private set; }

        public Task<CreateTorcedorSubscriptionCheckoutResult> CreateCheckoutAsync(
            Guid userId,
            Guid planId,
            TorcedorSubscriptionPaymentMethod paymentMethod,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ConfirmTorcedorSubscriptionPaymentResult> ConfirmPaymentAsync(
            Guid paymentId,
            string? webhookSecret,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ConfirmTorcedorSubscriptionPaymentResult> ConfirmPaymentAfterProviderSuccessAsync(
            Guid paymentId,
            string? providerPaymentReference,
            CancellationToken cancellationToken = default)
        {
            ConfirmAfterCalls++;
            return Task.FromResult(ConfirmTorcedorSubscriptionPaymentResult.Success());
        }
    }
}
