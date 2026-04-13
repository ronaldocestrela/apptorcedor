using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services.Membership;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class TorcedorMembershipSubscriptionServiceTests
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

    private static async Task<(AppDbContext db, Guid userId, Guid planId)> SeedUserAndPlanAsync(bool published, bool active = true)
    {
        var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.Users.Add(
            new ApplicationUser
            {
                Id = userId,
                UserName = "sub@test",
                NormalizedUserName = "SUB@TEST",
                Email = "sub@test",
                NormalizedEmail = "SUB@TEST",
                EmailConfirmed = true,
                Name = "Subscriber",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });

        db.MembershipPlans.Add(
            new MembershipPlanRecord
            {
                Id = planId,
                Name = "Plano Sub",
                Price = 50m,
                BillingCycle = "Monthly",
                DiscountPercentage = 0,
                IsActive = active,
                IsPublished = published,
                PublishedAt = published ? DateTimeOffset.UtcNow : null,
            });

        await db.SaveChangesAsync();
        return (db, userId, planId);
    }

    [Fact]
    public async Task Subscribe_creates_membership_pending_payment_and_history_when_no_row()
    {
        var (db, userId, planId) = await SeedUserAndPlanAsync(published: true);
        await using (db)
        {
            var sut = new TorcedorMembershipSubscriptionService(db);
            var result = await sut.SubscribeToPlanAsync(userId, planId, CancellationToken.None);

            Assert.True(result.Ok);
            Assert.Equal(MembershipStatus.PendingPayment, result.Status);

            var m = await db.Memberships.SingleAsync();
            Assert.Equal(userId, m.UserId);
            Assert.Equal(planId, m.PlanId);
            Assert.Equal(MembershipStatus.PendingPayment, m.Status);

            var h = await db.MembershipHistories.SingleAsync();
            Assert.Equal(MembershipHistoryEventTypes.Subscribed, h.EventType);
            Assert.Null(h.FromStatus);
            Assert.Equal(MembershipStatus.PendingPayment, h.ToStatus);
            Assert.Null(h.FromPlanId);
            Assert.Equal(planId, h.ToPlanId);
            Assert.Equal(userId, h.ActorUserId);
        }
    }

    [Fact]
    public async Task Subscribe_updates_nao_associado_to_pending_payment()
    {
        var (db, userId, planId) = await SeedUserAndPlanAsync(published: true);
        var mid = Guid.NewGuid();
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = mid,
                UserId = userId,
                PlanId = null,
                Status = MembershipStatus.NaoAssociado,
                StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            });
        await db.SaveChangesAsync();

        await using (db)
        {
            var sut = new TorcedorMembershipSubscriptionService(db);
            var result = await sut.SubscribeToPlanAsync(userId, planId, CancellationToken.None);
            Assert.True(result.Ok);

            var m = await db.Memberships.SingleAsync();
            Assert.Equal(mid, m.Id);
            Assert.Equal(MembershipStatus.PendingPayment, m.Status);
            Assert.Equal(planId, m.PlanId);

            var h = await db.MembershipHistories.SingleAsync();
            Assert.Equal(MembershipStatus.NaoAssociado, h.FromStatus);
            Assert.Equal(MembershipHistoryEventTypes.Subscribed, h.EventType);
        }
    }

    [Fact]
    public async Task Subscribe_fails_when_plan_not_published()
    {
        var (db, userId, planId) = await SeedUserAndPlanAsync(published: false);
        await using (db)
        {
            var sut = new TorcedorMembershipSubscriptionService(db);
            var result = await sut.SubscribeToPlanAsync(userId, planId, CancellationToken.None);
            Assert.False(result.Ok);
            Assert.Equal(SubscribeMemberError.PlanNotFoundOrNotAvailable, result.Error);
            Assert.False(await db.Memberships.AnyAsync());
        }
    }

    [Fact]
    public async Task Subscribe_fails_when_already_active()
    {
        var (db, userId, planId) = await SeedUserAndPlanAsync(published: true);
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = planId,
                Status = MembershipStatus.Ativo,
                StartDate = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        await using (db)
        {
            var sut = new TorcedorMembershipSubscriptionService(db);
            var result = await sut.SubscribeToPlanAsync(userId, planId, CancellationToken.None);
            Assert.False(result.Ok);
            Assert.Equal(SubscribeMemberError.AlreadyActiveSubscription, result.Error);
            Assert.False(await db.MembershipHistories.AnyAsync());
        }
    }

    [Fact]
    public async Task Subscribe_fails_when_pending_payment()
    {
        var (db, userId, planId) = await SeedUserAndPlanAsync(published: true);
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = planId,
                Status = MembershipStatus.PendingPayment,
                StartDate = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        await using (db)
        {
            var sut = new TorcedorMembershipSubscriptionService(db);
            var result = await sut.SubscribeToPlanAsync(userId, planId, CancellationToken.None);
            Assert.False(result.Ok);
            Assert.Equal(SubscribeMemberError.SubscriptionPendingPayment, result.Error);
        }
    }

    [Fact]
    public async Task Subscribe_fails_when_inadimplente()
    {
        var (db, userId, planId) = await SeedUserAndPlanAsync(published: true);
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = planId,
                Status = MembershipStatus.Inadimplente,
                StartDate = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        await using (db)
        {
            var sut = new TorcedorMembershipSubscriptionService(db);
            var result = await sut.SubscribeToPlanAsync(userId, planId, CancellationToken.None);
            Assert.False(result.Ok);
            Assert.Equal(SubscribeMemberError.MembershipStatusPreventsSubscribe, result.Error);
        }
    }
}
