using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class TorcedorPlanChangeServiceTests
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

    private static TorcedorSubscriptionCheckoutService CreateCheckoutService(AppDbContext db, string webhookSecret = "secret")
    {
        var mediator = new NoOpMediator();
        var loyalty = new NoOpLoyalty();
        var paymentProvider = new MockPaymentProvider();
        var opts = Microsoft.Extensions.Options.Options.Create(
            new Infrastructure.Options.PaymentWebhookOptions { WebhookSecret = webhookSecret });
        return new TorcedorSubscriptionCheckoutService(mediator, db, paymentProvider, loyalty, opts);
    }

    [Fact]
    public async Task ChangePlan_fails_when_no_membership()
    {
        await using var db = await CreateDbAsync();
        var sut = new TorcedorPlanChangeService(db, new MockPaymentProvider(), TimeProvider.System);
        var r = await sut.ChangePlanAsync(Guid.NewGuid(), Guid.NewGuid(), TorcedorSubscriptionPaymentMethod.Pix);
        Assert.False(r.Ok);
        Assert.Equal(ChangePlanError.MembershipNotFound, r.Error);
    }

    [Fact]
    public async Task ChangePlan_fails_when_not_active()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(MinPlan(planId, "P", 50m));
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

        var sut = new TorcedorPlanChangeService(db, new MockPaymentProvider(), TimeProvider.System);
        var r = await sut.ChangePlanAsync(userId, Guid.NewGuid(), TorcedorSubscriptionPaymentMethod.Pix);
        Assert.False(r.Ok);
        Assert.Equal(ChangePlanError.MembershipNotActive, r.Error);
    }

    [Fact]
    public async Task ChangePlan_fails_when_same_plan()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(MinPlan(planId, "P", 50m));
        db.Memberships.Add(ActiveMembership(userId, planId));
        await db.SaveChangesAsync();

        var sut = new TorcedorPlanChangeService(db, new MockPaymentProvider(), TimeProvider.System);
        var r = await sut.ChangePlanAsync(userId, planId, TorcedorSubscriptionPaymentMethod.Pix);
        Assert.False(r.Ok);
        Assert.Equal(ChangePlanError.SamePlan, r.Error);
    }

    [Fact]
    public async Task ChangePlan_fails_when_new_plan_unpublished()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planA = Guid.NewGuid();
        var planB = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(MinPlan(planA, "A", 50m));
        db.MembershipPlans.Add(
            new MembershipPlanRecord
            {
                Id = planB,
                Name = "B",
                Price = 100m,
                BillingCycle = "Monthly",
                DiscountPercentage = 0,
                IsActive = true,
                IsPublished = false,
            });
        db.Memberships.Add(ActiveMembership(userId, planA));
        await db.SaveChangesAsync();

        var sut = new TorcedorPlanChangeService(db, new MockPaymentProvider(), TimeProvider.System);
        var r = await sut.ChangePlanAsync(userId, planB, TorcedorSubscriptionPaymentMethod.Pix);
        Assert.False(r.Ok);
        Assert.Equal(ChangePlanError.PlanNotFoundOrNotAvailable, r.Error);
    }

    [Fact]
    public async Task ChangePlan_fails_when_next_due_date_missing()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planA = Guid.NewGuid();
        var planB = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(MinPlan(planA, "A", 50m));
        db.MembershipPlans.Add(MinPlan(planB, "B", 100m));
        var mid = Guid.NewGuid();
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = mid,
                UserId = userId,
                PlanId = planA,
                Status = MembershipStatus.Ativo,
                StartDate = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
                NextDueDate = null,
            });
        await db.SaveChangesAsync();

        var sut = new TorcedorPlanChangeService(db, new MockPaymentProvider(), TimeProvider.System);
        var r = await sut.ChangePlanAsync(userId, planB, TorcedorSubscriptionPaymentMethod.Pix);
        Assert.False(r.Ok);
        Assert.Equal(ChangePlanError.MissingBillingCycleContext, r.Error);
    }

    [Fact]
    public async Task ChangePlan_upgrade_mid_cycle_creates_proration_payment_and_history()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planA = Guid.NewGuid();
        var planB = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(MinPlan(planA, "A", 100m));
        db.MembershipPlans.Add(MinPlan(planB, "B", 200m));
        var mid = Guid.NewGuid();
        var cycleEnd = DateTimeOffset.Parse("2025-02-01T00:00:00Z");
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = mid,
                UserId = userId,
                PlanId = planA,
                Status = MembershipStatus.Ativo,
                StartDate = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
                NextDueDate = cycleEnd,
            });
        var openPayId = Guid.NewGuid();
        db.Payments.Add(
            new PaymentRecord
            {
                Id = openPayId,
                UserId = userId,
                MembershipId = mid,
                Amount = 100m,
                Status = PaymentChargeStatuses.Pending,
                DueDate = DateTimeOffset.UtcNow,
                PaymentMethod = "Pix",
                ExternalReference = openPayId.ToString("N"),
                ProviderName = "Mock",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                StatusReason = "Próxima mensalidade",
            });
        await db.SaveChangesAsync();

        var now = DateTimeOffset.Parse("2025-01-16T12:00:00Z");
        var sut = new TorcedorPlanChangeService(db, new MockPaymentProvider(), new FixedTimeProvider(now));
        var r = await sut.ChangePlanAsync(userId, planB, TorcedorSubscriptionPaymentMethod.Pix);

        Assert.True(r.Ok);
        Assert.NotNull(r.PaymentId);
        Assert.Equal(50m, r.ProrationAmount);
        Assert.Equal(planA, r.FromPlan!.PlanId);
        Assert.Equal(planB, r.ToPlan!.PlanId);

        var m = await db.Memberships.SingleAsync();
        Assert.Equal(planB, m.PlanId);
        Assert.Equal(MembershipStatus.Ativo, m.Status);

        var cancelled = await db.Payments.SingleAsync(p => p.Id == openPayId);
        Assert.Equal(PaymentChargeStatuses.Cancelled, cancelled.Status);

        var newPay = await db.Payments.SingleAsync(p => p.Id != openPayId);
        Assert.Equal(PaymentChargeStatuses.Pending, newPay.Status);
        Assert.StartsWith(TorcedorPlanChangePaymentReasons.ProrationPrefix, newPay.StatusReason ?? string.Empty);

        var h = await db.MembershipHistories.SingleAsync();
        Assert.Equal(MembershipHistoryEventTypes.PlanChanged, h.EventType);
        Assert.Equal(planA, h.FromPlanId);
        Assert.Equal(planB, h.ToPlanId);
        Assert.Equal(MembershipStatus.Ativo, h.FromStatus);
        Assert.Equal(MembershipStatus.Ativo, h.ToStatus);
    }

    [Fact]
    public async Task ChangePlan_downgrade_mid_cycle_has_zero_proration_and_no_new_payment()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planA = Guid.NewGuid();
        var planB = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(MinPlan(planA, "A", 200m));
        db.MembershipPlans.Add(MinPlan(planB, "B", 100m));
        var mid = Guid.NewGuid();
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = mid,
                UserId = userId,
                PlanId = planA,
                Status = MembershipStatus.Ativo,
                StartDate = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
                NextDueDate = DateTimeOffset.Parse("2025-02-01T00:00:00Z"),
            });
        await db.SaveChangesAsync();

        var now = DateTimeOffset.Parse("2025-01-16T12:00:00Z");
        var sut = new TorcedorPlanChangeService(db, new MockPaymentProvider(), new FixedTimeProvider(now));
        var r = await sut.ChangePlanAsync(userId, planB, TorcedorSubscriptionPaymentMethod.Pix);

        Assert.True(r.Ok);
        Assert.Equal(0m, r.ProrationAmount);
        Assert.Null(r.PaymentId);
        Assert.Null(r.Pix);

        var m = await db.Memberships.SingleAsync();
        Assert.Equal(planB, m.PlanId);
        Assert.False(await db.Payments.AnyAsync());
    }

    [Fact]
    public async Task Confirm_proration_payment_keeps_membership_active_and_marks_paid()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planA = Guid.NewGuid();
        var planB = Guid.NewGuid();
        db.Users.Add(MinUser(userId));
        db.MembershipPlans.Add(MinPlan(planA, "A", 100m));
        db.MembershipPlans.Add(MinPlan(planB, "B", 200m));
        var mid = Guid.NewGuid();
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = mid,
                UserId = userId,
                PlanId = planB,
                Status = MembershipStatus.Ativo,
                StartDate = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
                NextDueDate = DateTimeOffset.Parse("2025-02-01T00:00:00Z"),
            });
        var payId = Guid.NewGuid();
        db.Payments.Add(
            new PaymentRecord
            {
                Id = payId,
                UserId = userId,
                MembershipId = mid,
                Amount = 25m,
                Status = PaymentChargeStatuses.Pending,
                DueDate = DateTimeOffset.UtcNow,
                PaymentMethod = "Pix",
                ExternalReference = payId.ToString("N"),
                ProviderName = "Mock",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                StatusReason = TorcedorPlanChangePaymentReasons.ProrationPrefix,
            });
        await db.SaveChangesAsync();

        var checkout = CreateCheckoutService(db, "secret");
        var r = await checkout.ConfirmPaymentAsync(payId, "secret");
        Assert.True(r.Ok);

        var p = await db.Payments.SingleAsync();
        Assert.Equal(PaymentChargeStatuses.Paid, p.Status);
        var m = await db.Memberships.SingleAsync();
        Assert.Equal(MembershipStatus.Ativo, m.Status);
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

    private static MembershipPlanRecord MinPlan(Guid id, string name, decimal price) =>
        new()
        {
            Id = id,
            Name = name,
            Price = price,
            BillingCycle = "Monthly",
            DiscountPercentage = 0,
            IsActive = true,
            IsPublished = true,
            PublishedAt = DateTimeOffset.UtcNow,
        };

    private static MembershipRecord ActiveMembership(Guid userId, Guid planId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            Status = MembershipStatus.Ativo,
            StartDate = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
            NextDueDate = DateTimeOffset.Parse("2025-02-01T00:00:00Z"),
        };

    private sealed class NoOpMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }

    private sealed class NoOpLoyalty : ILoyaltyPointsTriggerPort
    {
        public Task AwardPointsForPaymentPaidAsync(Guid paymentId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AwardPointsForTicketPurchasedAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AwardPointsForTicketRedeemedAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utc;

        public FixedTimeProvider(DateTimeOffset utc) => _utc = utc;

        public override DateTimeOffset GetUtcNow() => _utc;
    }
}
