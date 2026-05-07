using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace AppTorcedor.Infrastructure.Tests.Services;

public sealed class TorcedorSubscriptionCheckoutServiceTests
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

    [Fact]
    public async Task Confirm_payment_for_recontract_cancels_legacy_open_charges()
    {
        await using var db = await CreateDbAsync();
        var now = DateTimeOffset.UtcNow;

        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var currentPaymentId = Guid.NewGuid();
        var legacyPendingId = Guid.NewGuid();
        var legacyOverdueId = Guid.NewGuid();

        db.Users.Add(
            new ApplicationUser
            {
                Id = userId,
                UserName = "checkout@test",
                NormalizedUserName = "CHECKOUT@TEST",
                Email = "checkout@test",
                NormalizedEmail = "CHECKOUT@TEST",
                EmailConfirmed = true,
                Name = "Checkout",
                IsActive = true,
                CreatedAt = now,
            });

        db.MembershipPlans.Add(
            new MembershipPlanRecord
            {
                Id = planId,
                Name = "Plano",
                Price = 99m,
                BillingCycle = "Monthly",
                DiscountPercentage = 0,
                IsActive = true,
                IsPublished = true,
                PublishedAt = now,
            });

        db.Memberships.Add(
            new MembershipRecord
            {
                Id = membershipId,
                UserId = userId,
                PlanId = planId,
                Status = MembershipStatus.PendingPayment,
                StartDate = now.AddDays(-2),
            });

        db.Payments.AddRange(
            new PaymentRecord
            {
                Id = currentPaymentId,
                UserId = userId,
                MembershipId = membershipId,
                Amount = 99m,
                Status = PaymentChargeStatuses.Pending,
                DueDate = now.AddDays(1),
                PaymentMethod = "Pix",
                ExternalReference = currentPaymentId.ToString("N"),
                ProviderName = "Mock",
                CreatedAt = now,
                UpdatedAt = now,
                StatusReason = "Cobranca atual",
            },
            new PaymentRecord
            {
                Id = legacyPendingId,
                UserId = userId,
                MembershipId = membershipId,
                Amount = 49m,
                Status = PaymentChargeStatuses.Pending,
                DueDate = now.AddDays(-20),
                PaymentMethod = "Pix",
                ExternalReference = legacyPendingId.ToString("N"),
                ProviderName = "Mock",
                CreatedAt = now.AddDays(-21),
                UpdatedAt = now.AddDays(-21),
                StatusReason = "Cobranca legada pending",
            },
            new PaymentRecord
            {
                Id = legacyOverdueId,
                UserId = userId,
                MembershipId = membershipId,
                Amount = 59m,
                Status = PaymentChargeStatuses.Overdue,
                DueDate = now.AddDays(-40),
                PaymentMethod = "Pix",
                ExternalReference = legacyOverdueId.ToString("N"),
                ProviderName = "Mock",
                CreatedAt = now.AddDays(-41),
                UpdatedAt = now.AddDays(-41),
                StatusReason = "Cobranca legada overdue",
            });

        await db.SaveChangesAsync();

        var paymentProvider = new Mock<IPaymentProvider>(MockBehavior.Strict);
        paymentProvider.SetupGet(x => x.ProviderKey).Returns("Mock");
        paymentProvider
            .Setup(x => x.CancelAsync(legacyPendingId, legacyPendingId.ToString("N"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        paymentProvider
            .Setup(x => x.CancelAsync(legacyOverdueId, legacyOverdueId.ToString("N"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var loyalty = new Mock<ILoyaltyPointsTriggerPort>(MockBehavior.Strict);
        loyalty
            .Setup(x => x.AwardPointsForPaymentPaidAsync(currentPaymentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new TorcedorSubscriptionCheckoutService(
            new NoOpMediator(),
            db,
            paymentProvider.Object,
            loyalty.Object,
            Microsoft.Extensions.Options.Options.Create(new Infrastructure.Options.PaymentsOptions { WebhookSecret = "secret" }));

        var result = await sut.ConfirmPaymentAfterProviderSuccessAsync(currentPaymentId, providerPaymentReference: null, CancellationToken.None);

        Assert.True(result.Ok);

        var currentPayment = await db.Payments.AsNoTracking().SingleAsync(p => p.Id == currentPaymentId);
        Assert.Equal(PaymentChargeStatuses.Paid, currentPayment.Status);

        var legacyPending = await db.Payments.AsNoTracking().SingleAsync(p => p.Id == legacyPendingId);
        Assert.Equal(PaymentChargeStatuses.Cancelled, legacyPending.Status);
        Assert.NotNull(legacyPending.CancelledAt);

        var legacyOverdue = await db.Payments.AsNoTracking().SingleAsync(p => p.Id == legacyOverdueId);
        Assert.Equal(PaymentChargeStatuses.Cancelled, legacyOverdue.Status);
        Assert.NotNull(legacyOverdue.CancelledAt);

        var membership = await db.Memberships.AsNoTracking().SingleAsync(m => m.Id == membershipId);
        Assert.Equal(MembershipStatus.Ativo, membership.Status);

        paymentProvider.Verify(
            x => x.CancelAsync(legacyPendingId, legacyPendingId.ToString("N"), It.IsAny<CancellationToken>()),
            Times.Once);
        paymentProvider.Verify(
            x => x.CancelAsync(legacyOverdueId, legacyOverdueId.ToString("N"), It.IsAny<CancellationToken>()),
            Times.Once);
        loyalty.Verify(x => x.AwardPointsForPaymentPaidAsync(currentPaymentId, It.IsAny<CancellationToken>()), Times.Once);
    }

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
            where TNotification : INotification =>
            Task.CompletedTask;
    }
}
