using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class AsaasWebhookProcessorTests
{
    [Fact]
    public async Task ProcessAsync_returns_ConfigurationError_when_webhook_token_not_configured()
    {
        await using var db = await CreateDbAsync();
        var checkout = new RecordingAsaasCheckoutPort();
        var opts = MsOptions.Create(new PaymentsOptions { Asaas = new PaymentsAsaasOptions { WebhookToken = "" } });
        var sut = new AsaasWebhookProcessor(db, checkout, opts, NullLogger<AsaasWebhookProcessor>.Instance);

        var r = await sut.ProcessAsync("{}", "any", CancellationToken.None);
        Assert.Equal(AsaasWebhookProcessResult.ConfigurationError, r);
        Assert.Equal(0, checkout.ConfirmAfterCalls);
    }

    [Fact]
    public async Task ProcessAsync_returns_Unauthorized_when_token_mismatches()
    {
        await using var db = await CreateDbAsync();
        var checkout = new RecordingAsaasCheckoutPort();
        var opts = MsOptions.Create(
            new PaymentsOptions { Asaas = new PaymentsAsaasOptions { WebhookToken = "expected" } });
        var sut = new AsaasWebhookProcessor(db, checkout, opts, NullLogger<AsaasWebhookProcessor>.Instance);

        var r = await sut.ProcessAsync("{}", "wrong", CancellationToken.None);
        Assert.Equal(AsaasWebhookProcessResult.Unauthorized, r);
        Assert.Equal(0, checkout.ConfirmAfterCalls);
    }

    [Fact]
    public async Task ProcessAsync_PAYMENT_RECEIVED_confirms_payment_and_is_idempotent()
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
                Email = "a@test.local",
                NormalizedEmail = "A@TEST.LOCAL",
                UserName = "a@test.local",
                NormalizedUserName = "A@TEST.LOCAL",
                Name = "A",
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
                ExternalReference = "plink_test",
                ProviderName = "Asaas",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        var checkout = new RecordingAsaasCheckoutPort();
        var opts = MsOptions.Create(
            new PaymentsOptions { Asaas = new PaymentsAsaasOptions { WebhookToken = "whsec_asaas" } });
        var sut = new AsaasWebhookProcessor(db, checkout, opts, NullLogger<AsaasWebhookProcessor>.Instance);

        var json =
            "{\"id\":\"evt_asaas_1\",\"event\":\"PAYMENT_RECEIVED\",\"payment\":{\"id\":\"pay_xyz\",\"externalReference\":\""
            + paymentId.ToString("D")
            + "\",\"value\":90}}";

        var r1 = await sut.ProcessAsync(json, "whsec_asaas", CancellationToken.None);
        var r2 = await sut.ProcessAsync(json, "whsec_asaas", CancellationToken.None);

        Assert.Equal(AsaasWebhookProcessResult.Ok, r1);
        Assert.Equal(AsaasWebhookProcessResult.Ok, r2);
        Assert.Equal(1, checkout.ConfirmAfterCalls);
        Assert.True(await db.ProcessedWebhookEvents.AnyAsync(e => e.Provider == "Asaas" && e.EventId == "evt_asaas_1"));
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

    private sealed class RecordingAsaasCheckoutPort : ITorcedorSubscriptionCheckoutPort
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
