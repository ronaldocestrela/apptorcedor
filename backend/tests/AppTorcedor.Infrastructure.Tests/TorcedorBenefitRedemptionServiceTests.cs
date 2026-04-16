using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services.Benefits;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class TorcedorBenefitRedemptionServiceTests
{
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

        var sut = new TorcedorBenefitRedemptionService(db);
        var r = await sut.RedeemOfferAsync(offerId, userId);

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

        var sut = new TorcedorBenefitRedemptionService(db);
        var r = await sut.RedeemOfferAsync(Guid.NewGuid(), userId);

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

        var sut = new TorcedorBenefitRedemptionService(db);
        var r = await sut.RedeemOfferAsync(offerId, userId);

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
            });
        await db.SaveChangesAsync();

        var sut = new TorcedorBenefitRedemptionService(db);
        var r = await sut.RedeemOfferAsync(offerId, userId);

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
    }
}
