using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Auditing;
using AppTorcedor.Infrastructure.Services.Governance;
using AppTorcedor.Infrastructure.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class TorcedorMembershipCancellationServiceTests
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

    private static IAppConfigurationPort ConfigWithCoolingOffDays(AppDbContext db, string value)
    {
        db.AppConfigurationEntries.Add(
            new AppConfigurationEntry
            {
                Key = TorcedorMembershipCancellationConfigKeys.CoolingOffDays,
                Value = value,
                Version = 1,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedByUserId = null,
            });
        return new AppConfigurationPort(db, new NoOpAudit());
    }

    [Fact]
    public async Task Cancel_fails_when_no_membership()
    {
        await using var db = await CreateDbAsync();
        var config = ConfigWithCoolingOffDays(db, "7");
        await db.SaveChangesAsync();
        var sut = new TorcedorMembershipCancellationService(db, new MockPaymentProvider(), TimeProvider.System, config);
        var r = await sut.CancelMembershipAsync(Guid.NewGuid());
        Assert.False(r.Ok);
        Assert.Equal(CancelMembershipError.MembershipNotFound, r.Error);
    }

    [Fact]
    public async Task Cancel_fails_when_already_cancelled()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = Guid.NewGuid(),
                Status = MembershipStatus.Cancelado,
                StartDate = DateTimeOffset.UtcNow.AddDays(-40),
            });
        var config = ConfigWithCoolingOffDays(db, "7");
        await db.SaveChangesAsync();
        var sut = new TorcedorMembershipCancellationService(db, new MockPaymentProvider(), TimeProvider.System, config);
        var r = await sut.CancelMembershipAsync(userId);
        Assert.False(r.Ok);
        Assert.Equal(CancelMembershipError.MembershipAlreadyCancelled, r.Error);
    }

    [Fact]
    public async Task Within_cooling_off_cancels_immediately_and_cancels_open_payments()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var mid = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(MinPlan(planId));
        var start = DateTimeOffset.Parse("2025-01-01T12:00:00Z");
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = mid,
                UserId = userId,
                PlanId = planId,
                Status = MembershipStatus.Ativo,
                StartDate = start,
                NextDueDate = DateTimeOffset.Parse("2025-02-01T12:00:00Z"),
            });
        var payId = Guid.NewGuid();
        db.Payments.Add(
            new PaymentRecord
            {
                Id = payId,
                UserId = userId,
                MembershipId = mid,
                Amount = 50m,
                Status = PaymentChargeStatuses.Pending,
                DueDate = DateTimeOffset.UtcNow,
                PaymentMethod = "Pix",
                ExternalReference = payId.ToString("N"),
                ProviderName = "Mock",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        var config = ConfigWithCoolingOffDays(db, "7");
        await db.SaveChangesAsync();

        var now = DateTimeOffset.Parse("2025-01-05T12:00:00Z");
        var sut = new TorcedorMembershipCancellationService(db, new MockPaymentProvider(), new FixedTime(now), config);
        var r = await sut.CancelMembershipAsync(userId);

        Assert.True(r.Ok);
        Assert.Equal(TorcedorMembershipCancellationMode.Immediate, r.Mode);
        Assert.Equal(MembershipStatus.Cancelado, r.MembershipStatus);

        var m = await db.Memberships.SingleAsync();
        Assert.Equal(MembershipStatus.Cancelado, m.Status);
        Assert.Null(m.NextDueDate);

        var p = await db.Payments.SingleAsync();
        Assert.Equal(PaymentChargeStatuses.Cancelled, p.Status);

        var h = await db.MembershipHistories.SingleAsync();
        Assert.Equal(MembershipHistoryEventTypes.CancelledByMember, h.EventType);
    }

    [Fact]
    public async Task Outside_cooling_off_schedules_end_of_cycle_and_keeps_active()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var mid = Guid.NewGuid();
        var nextDue = DateTimeOffset.Parse("2025-03-01T12:00:00Z");
        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(MinPlan(planId));
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = mid,
                UserId = userId,
                PlanId = planId,
                Status = MembershipStatus.Ativo,
                StartDate = DateTimeOffset.Parse("2025-01-01T12:00:00Z"),
                NextDueDate = nextDue,
            });
        var config = ConfigWithCoolingOffDays(db, "7");
        await db.SaveChangesAsync();

        var now = DateTimeOffset.Parse("2025-02-15T12:00:00Z");
        var sut = new TorcedorMembershipCancellationService(db, new MockPaymentProvider(), new FixedTime(now), config);
        var r = await sut.CancelMembershipAsync(userId);

        Assert.True(r.Ok);
        Assert.Equal(TorcedorMembershipCancellationMode.ScheduledEndOfCycle, r.Mode);
        Assert.Equal(MembershipStatus.Ativo, r.MembershipStatus);
        Assert.Equal(nextDue, r.AccessValidUntilUtc);

        var m = await db.Memberships.SingleAsync();
        Assert.Equal(MembershipStatus.Ativo, m.Status);
        Assert.Equal(nextDue, m.EndDate);
 }

    [Fact]
    public async Task Second_cancel_when_scheduled_fails()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var mid = Guid.NewGuid();
        var nextDue = DateTimeOffset.Parse("2025-03-01T12:00:00Z");
        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(MinPlan(planId));
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = mid,
                UserId = userId,
                PlanId = planId,
                Status = MembershipStatus.Ativo,
                StartDate = DateTimeOffset.Parse("2025-01-01T12:00:00Z"),
                NextDueDate = nextDue,
                EndDate = nextDue,
            });
        var config = ConfigWithCoolingOffDays(db, "7");
        await db.SaveChangesAsync();

        var sut = new TorcedorMembershipCancellationService(db, new MockPaymentProvider(), new FixedTime(DateTimeOffset.Parse("2025-02-15T12:00:00Z")), config);
        var r = await sut.CancelMembershipAsync(userId);
        Assert.False(r.Ok);
        Assert.Equal(CancelMembershipError.CancellationAlreadyScheduled, r.Error);
    }

    [Fact]
    public async Task Effective_sweep_marks_scheduled_membership_cancelled_after_end_date()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var mid = Guid.NewGuid();
        var end = DateTimeOffset.Parse("2025-03-01T12:00:00Z");
        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(MinPlan(planId));
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = mid,
                UserId = userId,
                PlanId = planId,
                Status = MembershipStatus.Ativo,
                StartDate = DateTimeOffset.Parse("2025-01-01T12:00:00Z"),
                NextDueDate = end,
                EndDate = end,
            });
        await db.SaveChangesAsync();

        var sweep = new MembershipScheduledCancellationEffectiveSweep(db);
        var now = DateTimeOffset.Parse("2025-03-02T12:00:00Z");
        var n = await sweep.ApplyAsync(now, CancellationToken.None);
        Assert.Equal(1, n);

        var m = await db.Memberships.SingleAsync();
        Assert.Equal(MembershipStatus.Cancelado, m.Status);
        var lastHist = await db.MembershipHistories.OrderByDescending(h => h.CreatedAt).FirstAsync();
        Assert.Equal(MembershipHistoryEventTypes.StatusChanged, lastHist.EventType);
    }

    [Fact]
    public async Task Invalid_config_falls_back_to_default_cooling_off_days()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var mid = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(MinPlan(planId));
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = mid,
                UserId = userId,
                PlanId = planId,
                Status = MembershipStatus.Ativo,
                StartDate = DateTimeOffset.Parse("2025-01-01T12:00:00Z"),
                NextDueDate = DateTimeOffset.Parse("2025-02-01T12:00:00Z"),
            });
        var config = ConfigWithCoolingOffDays(db, "not-a-number");
        await db.SaveChangesAsync();

        var now = DateTimeOffset.Parse("2025-01-05T12:00:00Z");
        var sut = new TorcedorMembershipCancellationService(db, new MockPaymentProvider(), new FixedTime(now), config);
        var r = await sut.CancelMembershipAsync(userId);
        Assert.True(r.Ok);
        Assert.Equal(TorcedorMembershipCancellationMode.Immediate, r.Mode);
    }

    private static ApplicationUser MinUser(Guid id) =>
        new()
        {
            Id = id,
            UserName = "u@test",
            NormalizedUserName = "U@TEST",
            Email = "u@test",
            NormalizedEmail = "U@TEST",
            EmailConfirmed = true,
            Name = "U",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    private static MembershipPlanRecord MinPlan(Guid id) =>
        new()
        {
            Id = id,
            Name = "P",
            Price = 50m,
            BillingCycle = "Monthly",
            DiscountPercentage = 0,
            IsActive = true,
            IsPublished = true,
            PublishedAt = DateTimeOffset.UtcNow,
        };

    private sealed class FixedTime(DateTimeOffset utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utc;
    }

    private sealed class NoOpAudit : ICurrentAuditContext
    {
        public Guid? UserId { get; set; }
        public string? CorrelationId { get; set; }
    }
}
