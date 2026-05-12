using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services.Benefits;
using AppTorcedor.Infrastructure.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class TorcedorBenefitRedemptionServiceTests
{
    private static TorcedorShirtRedemptionRequest SampleCarrierShirtRequest() =>
        new(
            "M",
            "Home",
            "10",
            "Fulano",
            "01310100",
            "Bela Vista",
            "Av Paulista",
            "1000",
            "São Paulo",
            "SP",
            TorcedorBenefitShippingMethods.Carrier,
            2,
            "Correios",
            "SEDEX",
            12.68m,
            2);

    private static TorcedorShirtRedemptionRequest SamplePickupShirtRequest() =>
        new(
            "M",
            "Home",
            "10",
            "Fulano",
            "",
            "",
            "",
            "",
            "",
            "",
            TorcedorBenefitShippingMethods.Pickup);

    private static async Task<AppDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static ApplicationUser MinUser(Guid id) =>
        new()
        {
            Id = id,
            UserName = $"{id:N}@t",
            NormalizedUserName = $"{id:N}@T",
            Email = $"{id:N}@t",
            NormalizedEmail = $"{id:N}@T",
            EmailConfirmed = true,
            Name = "T",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    [Fact]
    public async Task Redeem_succeeds_when_open_offer_and_user_eligible()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Users.Add(MinUser(userId));
        db.BenefitPartners.Add(
            new BenefitPartnerRecord
            {
                Id = partnerId,
                Name = "P",
                Description = null,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        db.BenefitOffers.Add(
            new BenefitOfferRecord
            {
                Id = offerId,
                PartnerId = partnerId,
                Title = "O",
                Description = "d",
                IsActive = true,
                StartAt = now.AddDays(-1),
                EndAt = now.AddDays(30),
                CreatedAt = now,
                UpdatedAt = now,
            });
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var r = await sut.RedeemOfferAsync(offerId, userId, null);

        Assert.True(r.Ok);
        Assert.NotNull(r.RedemptionId);
        Assert.True(await db.BenefitRedemptions.AnyAsync(x => x.Id == r.RedemptionId));
    }

    [Fact]
    public async Task Redeem_fails_not_found_when_offer_missing()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var r = await sut.RedeemOfferAsync(Guid.NewGuid(), userId, null);

        Assert.False(r.Ok);
        Assert.Equal(TorcedorRedemptionError.NotFound, r.Error);
    }

    [Fact]
    public async Task Redeem_fails_not_eligible_when_plan_restriction_not_met()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(
            new MembershipPlanRecord
            {
                Id = planId,
                Name = "Pl",
                Price = 10m,
                BillingCycle = "Monthly",
                DiscountPercentage = 0,
                IsActive = true,
                IsPublished = true,
            });
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = null,
                Status = MembershipStatus.Ativo,
                StartDate = now,
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
                Title = "O",
                IsActive = true,
                StartAt = now.AddDays(-1),
                EndAt = now.AddDays(30),
                CreatedAt = now,
                UpdatedAt = now,
            });
        db.BenefitOfferPlanEligibilities.Add(new BenefitOfferPlanEligibilityRecord { OfferId = offerId, PlanId = planId });
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var r = await sut.RedeemOfferAsync(offerId, userId, null);

        Assert.False(r.Ok);
        Assert.Equal(TorcedorRedemptionError.NotEligible, r.Error);
    }

    [Fact]
    public async Task Redeem_fails_already_redeemed()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Users.Add(MinUser(userId));
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
                Title = "O",
                IsActive = true,
                StartAt = now.AddDays(-1),
                EndAt = now.AddDays(30),
                CreatedAt = now,
                UpdatedAt = now,
            });
        db.BenefitRedemptions.Add(
            new BenefitRedemptionRecord
            {
                Id = Guid.NewGuid(),
                OfferId = offerId,
                UserId = userId,
                ActorUserId = Guid.NewGuid(),
                CreatedAt = now,
                Status = BenefitRedemptionStatus.Approved,
            });
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var r = await sut.RedeemOfferAsync(offerId, userId, null);

        Assert.False(r.Ok);
        Assert.Equal(TorcedorRedemptionError.AlreadyRedeemed, r.Error);
    }

    [Fact]
    public async Task GetEligibleOfferDetail_returns_detail_with_redemption_flag()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Users.Add(MinUser(userId));
        db.BenefitPartners.Add(
            new BenefitPartnerRecord
            {
                Id = partnerId,
                Name = "Parc",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        db.BenefitOffers.Add(
            new BenefitOfferRecord
            {
                Id = offerId,
                PartnerId = partnerId,
                Title = "Tit",
                Description = "Desc",
                IsActive = true,
                StartAt = now.AddDays(-1),
                EndAt = now.AddDays(30),
                CreatedAt = now,
                UpdatedAt = now,
            });
        await db.SaveChangesAsync();

        var read = new TorcedorBenefitsReadService(db);
        var d = await read.GetEligibleOfferDetailAsync(userId, offerId);

        Assert.NotNull(d);
        Assert.Equal(offerId, d.OfferId);
        Assert.Equal("Parc", d.PartnerName);
        Assert.False(d.AlreadyRedeemed);
        Assert.Null(d.RedemptionDateUtc);
        Assert.False(d.IsShirtCustomizationOffer);
        Assert.Empty(d.ShirtSizes);
        Assert.Empty(d.ShirtModels);
        Assert.Equal("none", d.RedemptionWorkflowStatus);
    }

    [Fact]
    public async Task Shirt_redeem_creates_pending_when_catalog_configured()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Users.Add(MinUser(userId));
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
        db.BenefitShirtCatalogOptions.Add(
            new BenefitShirtCatalogOptionRecord
            {
                Id = Guid.NewGuid(),
                OfferId = offerId,
                Kind = BenefitShirtCatalogOptionKind.Size,
                Value = "M",
                SortOrder = 0,
            });
        db.BenefitShirtCatalogOptions.Add(
            new BenefitShirtCatalogOptionRecord
            {
                Id = Guid.NewGuid(),
                OfferId = offerId,
                Kind = BenefitShirtCatalogOptionKind.Model,
                Value = "Home",
                SortOrder = 0,
            });
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var shirt = SampleCarrierShirtRequest();
        var r = await sut.RedeemOfferAsync(offerId, userId, shirt);

        Assert.True(r.Ok);
        var row = await db.BenefitRedemptions.SingleAsync(x => x.Id == r.RedemptionId);
        Assert.NotNull(row.ShippingPaymentId);
        Assert.Equal(
            $"https://mock-payments.local/checkout/{row.ShippingPaymentId:N}?amount=12.68&currency=BRL",
            r.CheckoutUrl);
        Assert.Equal(BenefitRedemptionStatus.Pending, row.Status);
        Assert.Equal("M", row.ShirtSize);
        Assert.Equal("10", row.ShirtNumber);
        Assert.Equal("01310100", row.DeliveryCep);
        Assert.Equal("Av Paulista", row.DeliveryStreet);
        Assert.Equal("SP", row.DeliveryState);
        Assert.Equal(TorcedorBenefitShippingMethods.Carrier, row.ShippingMethod);
        Assert.Equal(2, row.ShippingCarrierId);
        Assert.Equal("SEDEX", row.ShippingServiceName);
        var pay = await db.Payments.SingleAsync(p => p.Id == row.ShippingPaymentId!.Value);
        Assert.Equal(12.68m, pay.Amount);
        Assert.Equal("Mock", pay.ProviderName);
    }

    [Fact]
    public async Task Shirt_redeem_pickup_creates_pending_without_address()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Users.Add(MinUser(userId));
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
        db.BenefitShirtCatalogOptions.Add(
            new BenefitShirtCatalogOptionRecord
            {
                Id = Guid.NewGuid(),
                OfferId = offerId,
                Kind = BenefitShirtCatalogOptionKind.Size,
                Value = "M",
                SortOrder = 0,
            });
        db.BenefitShirtCatalogOptions.Add(
            new BenefitShirtCatalogOptionRecord
            {
                Id = Guid.NewGuid(),
                OfferId = offerId,
                Kind = BenefitShirtCatalogOptionKind.Model,
                Value = "Home",
                SortOrder = 0,
            });
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var shirt = SamplePickupShirtRequest();
        var r = await sut.RedeemOfferAsync(offerId, userId, shirt);

        Assert.True(r.Ok);
        var row = await db.BenefitRedemptions.SingleAsync(x => x.Id == r.RedemptionId);
        Assert.Equal(BenefitRedemptionStatus.Pending, row.Status);
        Assert.Equal(TorcedorBenefitShippingMethods.Pickup, row.ShippingMethod);
        Assert.Null(row.DeliveryCep);
        Assert.Null(row.ShippingCarrierId);
    }

    [Fact]
    public async Task Shirt_redeem_fails_validation_when_delivery_incomplete()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Users.Add(MinUser(userId));
        db.BenefitPartners.Add(
            new BenefitPartnerRecord { Id = partnerId, Name = "P", IsActive = true, CreatedAt = now, UpdatedAt = now });
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
        db.BenefitShirtCatalogOptions.Add(
            new BenefitShirtCatalogOptionRecord
            {
                Id = Guid.NewGuid(),
                OfferId = offerId,
                Kind = BenefitShirtCatalogOptionKind.Size,
                Value = "M",
                SortOrder = 0,
            });
        db.BenefitShirtCatalogOptions.Add(
            new BenefitShirtCatalogOptionRecord
            {
                Id = Guid.NewGuid(),
                OfferId = offerId,
                Kind = BenefitShirtCatalogOptionKind.Model,
                Value = "Home",
                SortOrder = 0,
            });
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var shirt = new TorcedorShirtRedemptionRequest(
            "M",
            "Home",
            "10",
            "Fulano",
            "",
            "Bela Vista",
            "Av Paulista",
            "1000",
            "São Paulo",
            "SP",
            TorcedorBenefitShippingMethods.Carrier);
        var r = await sut.RedeemOfferAsync(offerId, userId, shirt);

        Assert.False(r.Ok);
        Assert.Equal(TorcedorRedemptionError.Validation, r.Error);
    }

    [Fact]
    public async Task Shirt_redeem_fails_validation_without_catalog()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Users.Add(MinUser(userId));
        db.BenefitPartners.Add(
            new BenefitPartnerRecord { Id = partnerId, Name = "P", IsActive = true, CreatedAt = now, UpdatedAt = now });
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
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var shirt = SampleCarrierShirtRequest();
        var r = await sut.RedeemOfferAsync(offerId, userId, shirt);

        Assert.False(r.Ok);
        Assert.Equal(TorcedorRedemptionError.Validation, r.Error);
    }

    [Fact]
    public async Task Non_shirt_redeem_fails_when_payload_provided()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Users.Add(MinUser(userId));
        db.BenefitPartners.Add(
            new BenefitPartnerRecord { Id = partnerId, Name = "P", IsActive = true, CreatedAt = now, UpdatedAt = now });
        db.BenefitOffers.Add(
            new BenefitOfferRecord
            {
                Id = offerId,
                PartnerId = partnerId,
                Title = "O",
                IsActive = true,
                StartAt = now.AddDays(-1),
                EndAt = now.AddDays(30),
                CreatedAt = now,
                UpdatedAt = now,
                IsShirtCustomizationOffer = false,
            });
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var shirt = SampleCarrierShirtRequest();
        var r = await sut.RedeemOfferAsync(offerId, userId, shirt);

        Assert.False(r.Ok);
        Assert.Equal(TorcedorRedemptionError.Validation, r.Error);
    }

    // ──────────────────────── CancelMyRedemptionAsync ────────────────────────

    private static async Task<(Guid PartnerId, Guid OfferId)> SeedOpenOfferAsync(
        AppDbContext db,
        Guid userId,
        bool isShirt = false)
    {
        var partnerId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.BenefitPartners.Add(
            new BenefitPartnerRecord
            {
                Id = partnerId,
                Name = "P-Cancel",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        db.BenefitOffers.Add(
            new BenefitOfferRecord
            {
                Id = offerId,
                PartnerId = partnerId,
                Title = "O-Cancel",
                IsActive = true,
                StartAt = now.AddDays(-1),
                EndAt = now.AddDays(30),
                CreatedAt = now,
                UpdatedAt = now,
                IsShirtCustomizationOffer = isShirt,
            });
        if (isShirt)
        {
            db.BenefitShirtCatalogOptions.Add(new BenefitShirtCatalogOptionRecord
            {
                Id = Guid.NewGuid(), OfferId = offerId, Kind = BenefitShirtCatalogOptionKind.Size, Value = "M", SortOrder = 0,
            });
            db.BenefitShirtCatalogOptions.Add(new BenefitShirtCatalogOptionRecord
            {
                Id = Guid.NewGuid(), OfferId = offerId, Kind = BenefitShirtCatalogOptionKind.Model, Value = "Home", SortOrder = 0,
            });
        }

        await db.SaveChangesAsync();
        return (partnerId, offerId);
    }

    [Fact]
    public async Task CancelMyRedemption_cancels_approved_redemption()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        var (_, offerId) = await SeedOpenOfferAsync(db, userId);

        var redemptionId = Guid.NewGuid();
        db.BenefitRedemptions.Add(
            new BenefitRedemptionRecord
            {
                Id = redemptionId,
                OfferId = offerId,
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = BenefitRedemptionStatus.Approved,
            });
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var r = await sut.CancelMyRedemptionAsync(offerId, userId);

        Assert.True(r.Ok);
        var row = await db.BenefitRedemptions.SingleAsync(x => x.Id == redemptionId);
        Assert.Equal(BenefitRedemptionStatus.CancelledByUser, row.Status);
        Assert.NotNull(row.CancelledByUserAtUtc);
    }

    [Fact]
    public async Task CancelMyRedemption_cancels_pending_redemption()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        var (_, offerId) = await SeedOpenOfferAsync(db, userId);

        var redemptionId = Guid.NewGuid();
        db.BenefitRedemptions.Add(
            new BenefitRedemptionRecord
            {
                Id = redemptionId,
                OfferId = offerId,
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = BenefitRedemptionStatus.Pending,
            });
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var r = await sut.CancelMyRedemptionAsync(offerId, userId);

        Assert.True(r.Ok);
        var row = await db.BenefitRedemptions.SingleAsync(x => x.Id == redemptionId);
        Assert.Equal(BenefitRedemptionStatus.CancelledByUser, row.Status);
        Assert.NotNull(row.CancelledByUserAtUtc);
    }

    [Fact]
    public async Task CancelMyRedemption_returns_not_found_when_no_active_redemption()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        var (_, offerId) = await SeedOpenOfferAsync(db, userId);
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var r = await sut.CancelMyRedemptionAsync(offerId, userId);

        Assert.False(r.Ok);
        Assert.Equal(TorcedorRedemptionCancelError.NotFound, r.Error);
    }

    [Fact]
    public async Task CancelMyRedemption_returns_not_cancellable_when_freight_already_paid()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        var (_, offerId) = await SeedOpenOfferAsync(db, userId, isShirt: true);

        var redemptionId = Guid.NewGuid();
        db.BenefitRedemptions.Add(
            new BenefitRedemptionRecord
            {
                Id = redemptionId,
                OfferId = offerId,
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = BenefitRedemptionStatus.Pending,
                ShippingMethod = TorcedorBenefitShippingMethods.Carrier,
                ShippingPaymentId = Guid.NewGuid(),
                ShippingPaidAtUtc = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var r = await sut.CancelMyRedemptionAsync(offerId, userId);

        Assert.False(r.Ok);
        Assert.Equal(TorcedorRedemptionCancelError.NotCancellable, r.Error);
    }

    [Fact]
    public async Task After_cancellation_new_redemption_is_placed_in_pending()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        var (_, offerId) = await SeedOpenOfferAsync(db, userId);

        // First: redeem (normal offer → goes to Approved)
        var sut = new TorcedorBenefitRedemptionService(db, new MockPaymentProvider());
        var r1 = await sut.RedeemOfferAsync(offerId, userId, null);
        Assert.True(r1.Ok);
        var row1 = await db.BenefitRedemptions.SingleAsync(x => x.Id == r1.RedemptionId);
        Assert.Equal(BenefitRedemptionStatus.Approved, row1.Status);

        // Cancel it
        var cancel = await sut.CancelMyRedemptionAsync(offerId, userId);
        Assert.True(cancel.Ok);

        // Re-redeem → must be Pending (awaiting staff approval)
        var r2 = await sut.RedeemOfferAsync(offerId, userId, null);
        Assert.True(r2.Ok);
        var row2 = await db.BenefitRedemptions.SingleAsync(x => x.Id == r2.RedemptionId);
        Assert.Equal(BenefitRedemptionStatus.Pending, row2.Status);
    }

    [Fact]
    public async Task GetEligibleOfferDetail_shows_cancelled_workflow_status_and_requires_approval_flag()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        var (_, offerId) = await SeedOpenOfferAsync(db, userId);

        db.BenefitRedemptions.Add(
            new BenefitRedemptionRecord
            {
                Id = Guid.NewGuid(),
                OfferId = offerId,
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                Status = BenefitRedemptionStatus.CancelledByUser,
                CancelledByUserAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            });
        await db.SaveChangesAsync();

        var read = new TorcedorBenefitsReadService(db);
        var d = await read.GetEligibleOfferDetailAsync(userId, offerId);

        Assert.NotNull(d);
        Assert.Equal("cancelled", d.RedemptionWorkflowStatus);
        Assert.False(d.AlreadyRedeemed);
        Assert.True(d.RequiresApprovalForNextRedemption);
    }
}
