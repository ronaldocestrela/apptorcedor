using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SocioTorcedor.Modules.Backoffice.Domain.Enums;
using SocioTorcedor.Modules.Payments.Application.Commands.ProcessTenantSaasWebhook;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Services;
using StripeWebhookHandlingOptions = SocioTorcedor.Modules.Payments.Application.Options.StripeWebhookHandlingOptions;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Tests.Commands;

public sealed class ProcessTenantSaasWebhookHandlerTests
{
    private static ProcessTenantSaasWebhookHandler CreateHandler(
        ITenantMasterPaymentsRepository repo,
        StripeWebhookHandlingOptions? webhookOptions = null) =>
        new(
            repo,
            new TenantSaasStripeWebhookEffectApplicator(
                repo,
                Microsoft.Extensions.Options.Options.Create(webhookOptions ?? new StripeWebhookHandlingOptions()),
                NullLogger<TenantSaasStripeWebhookEffectApplicator>.Instance),
            Microsoft.Extensions.Options.Options.Create(webhookOptions ?? new StripeWebhookHandlingOptions()));

    private static TenantBillingSubscription MonthlySub(string externalId, Guid? tenantId = null)
    {
        var tid = tenantId ?? Guid.NewGuid();
        return TenantBillingSubscription.Start(
            tid,
            Guid.NewGuid(),
            Guid.NewGuid(),
            BillingCycle.Monthly,
            99m,
            "BRL",
            null,
            externalId,
            BillingSubscriptionStatus.Active,
            null);
    }

    [Fact]
    public async Task Legacy_invoice_paid_marks_open_invoice_and_extends_subscription()
    {
        var sub = MonthlySub("sub_legacy_1");
        var invoice = TenantBillingInvoice.Create(sub.Id, 99m, "BRL", DateTime.UtcNow.AddDays(1), BillingInvoiceStatus.Open, null);

        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetWebhookByIdempotencyKeyAsync("evt_1", Arg.Any<CancellationToken>()).Returns((TenantPaymentWebhookInbox?)null);
        repo.GetSubscriptionByExternalIdAsync("sub_legacy_1", Arg.Any<CancellationToken>()).Returns(sub);
        repo.ListInvoicesByTenantAsync(sub.TenantId, 0, 50, Arg.Any<CancellationToken>())
            .Returns(new[] { invoice });

        var handler = CreateHandler(repo);
        var body = """{"eventType":"invoice.paid","externalSubscriptionId":"sub_legacy_1"}""";

        var r = await handler.Handle(new ProcessTenantSaasWebhookCommand("evt_1", "invoice.paid", body), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(BillingInvoiceStatus.Paid);
        sub.Status.Should().Be(BillingSubscriptionStatus.Active);
        sub.NextBillingAtUtc.Should().NotBeNull();
        await repo.Received(1).AddWebhookAsync(Arg.Any<TenantPaymentWebhookInbox>(), Arg.Any<CancellationToken>());
        await repo.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stripe_invoice_paid_shape_marks_open_invoice()
    {
        var sub = MonthlySub("sub_stripe_1");
        var invoice = TenantBillingInvoice.Create(sub.Id, 50m, "BRL", DateTime.UtcNow.AddDays(1), BillingInvoiceStatus.Draft, null);

        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetWebhookByIdempotencyKeyAsync("evt_stripe", Arg.Any<CancellationToken>()).Returns((TenantPaymentWebhookInbox?)null);
        repo.GetSubscriptionByExternalIdAsync("sub_stripe_1", Arg.Any<CancellationToken>()).Returns(sub);
        repo.ListInvoicesByTenantAsync(sub.TenantId, 0, 50, Arg.Any<CancellationToken>())
            .Returns(new[] { invoice });

        var handler = CreateHandler(repo);
        var body = """
            {"type":"invoice.paid","data":{"object":{"subscription":"sub_stripe_1"}}}
            """;

        var r = await handler.Handle(new ProcessTenantSaasWebhookCommand("evt_stripe", "invoice.paid", body), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(BillingInvoiceStatus.Paid);
        sub.Status.Should().Be(BillingSubscriptionStatus.Active);
    }

    [Fact]
    public async Task Stripe_invoice_payment_failed_sets_past_due()
    {
        var sub = MonthlySub("sub_fail");

        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetWebhookByIdempotencyKeyAsync("evt_fail", Arg.Any<CancellationToken>()).Returns((TenantPaymentWebhookInbox?)null);
        repo.GetSubscriptionByExternalIdAsync("sub_fail", Arg.Any<CancellationToken>()).Returns(sub);

        var handler = CreateHandler(repo);
        var body = """
            {"type":"invoice.payment_failed","data":{"object":{"subscription":"sub_fail"}}}
            """;

        var r = await handler.Handle(new ProcessTenantSaasWebhookCommand("evt_fail", "invoice.payment_failed", body), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        sub.Status.Should().Be(BillingSubscriptionStatus.PastDue);
    }

    [Fact]
    public async Task Stripe_subscription_updated_active_sets_period_end_from_unix_timestamp()
    {
        var sub = MonthlySub("sub_period");
        var endUnix = new DateTimeOffset(2030, 6, 15, 12, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetWebhookByIdempotencyKeyAsync("evt_sub", Arg.Any<CancellationToken>()).Returns((TenantPaymentWebhookInbox?)null);
        repo.GetSubscriptionByExternalIdAsync("sub_period", Arg.Any<CancellationToken>()).Returns(sub);

        var handler = CreateHandler(repo);
        var body =
            "{\"type\":\"customer.subscription.updated\",\"data\":{\"object\":{\"id\":\"sub_period\",\"status\":\"active\",\"current_period_end\":" +
            endUnix +
            "}}}";

        var r = await handler.Handle(new ProcessTenantSaasWebhookCommand("evt_sub", "customer.subscription.updated", body), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        sub.Status.Should().Be(BillingSubscriptionStatus.Active);
        sub.CurrentPeriodEndUtc.Should().BeCloseTo(DateTimeOffset.FromUnixTimeSeconds(endUnix).UtcDateTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Second_delivery_with_processed_inbox_is_idempotent()
    {
        var sub = MonthlySub("sub_idem");
        var invoice = TenantBillingInvoice.Create(sub.Id, 10m, "BRL", DateTime.UtcNow.AddDays(1), BillingInvoiceStatus.Open, null);

        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        TenantPaymentWebhookInbox? inbox = null;
        repo.GetWebhookByIdempotencyKeyAsync("evt_idem", Arg.Any<CancellationToken>())
            .Returns(_ => inbox);
        repo.AddWebhookAsync(Arg.Do<TenantPaymentWebhookInbox>(i => inbox = i), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        repo.GetSubscriptionByExternalIdAsync("sub_idem", Arg.Any<CancellationToken>()).Returns(sub);
        repo.ListInvoicesByTenantAsync(sub.TenantId, 0, 50, Arg.Any<CancellationToken>())
            .Returns(new[] { invoice });

        var handler = CreateHandler(repo);
        var body = """{"eventType":"invoice.paid","externalSubscriptionId":"sub_idem"}""";

        (await handler.Handle(new ProcessTenantSaasWebhookCommand("evt_idem", "invoice.paid", body), CancellationToken.None)).IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(BillingInvoiceStatus.Paid);

        (await handler.Handle(new ProcessTenantSaasWebhookCommand("evt_idem", "invoice.paid", body), CancellationToken.None)).IsSuccess.Should().BeTrue();

        await repo.Received(1).GetSubscriptionByExternalIdAsync("sub_idem", Arg.Any<CancellationToken>());
        await repo.Received(1).AddWebhookAsync(Arg.Any<TenantPaymentWebhookInbox>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Thin events use <c>snapshot_event</c> as idempotency key when present, matching the snapshot <c>event.id</c>.
    /// </summary>
    [Fact]
    public async Task When_inbox_already_processed_with_shared_key_second_delivery_is_no_op()
    {
        var sub = MonthlySub("sub_cross");
        var invoice = TenantBillingInvoice.Create(sub.Id, 10m, "BRL", DateTime.UtcNow.AddDays(1), BillingInvoiceStatus.Open, null);

        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        var processed = TenantPaymentWebhookInbox.Receive("evt_snapshot_shared", "invoice.paid", "{}");
        processed.MarkProcessed();
        repo.GetWebhookByIdempotencyKeyAsync("evt_snapshot_shared", Arg.Any<CancellationToken>()).Returns(processed);

        var handler = CreateHandler(repo);
        var body = """{"type":"invoice.paid","data":{"object":{"subscription":"sub_cross"}}}""";

        var r = await handler.Handle(
            new ProcessTenantSaasWebhookCommand("evt_snapshot_shared", "invoice.paid", body),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(BillingInvoiceStatus.Open);
        await repo.DidNotReceive().AddWebhookAsync(Arg.Any<TenantPaymentWebhookInbox>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().GetSubscriptionByExternalIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Shadow_mode_skips_inbox_and_domain_effects()
    {
        var sub = MonthlySub("sub_shadow");
        var invoice = TenantBillingInvoice.Create(sub.Id, 10m, "BRL", DateTime.UtcNow.AddDays(1), BillingInvoiceStatus.Open, null);

        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetSubscriptionByExternalIdAsync("sub_shadow", Arg.Any<CancellationToken>()).Returns(sub);
        repo.ListInvoicesByTenantAsync(sub.TenantId, 0, 50, Arg.Any<CancellationToken>())
            .Returns(new[] { invoice });

        var handler = CreateHandler(repo, new StripeWebhookHandlingOptions { StripeWebhookShadowMode = true });
        var body = """{"eventType":"invoice.paid","externalSubscriptionId":"sub_shadow"}""";

        var r = await handler.Handle(new ProcessTenantSaasWebhookCommand("evt_shadow", "invoice.paid", body), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(BillingInvoiceStatus.Open);
        await repo.DidNotReceive().GetWebhookByIdempotencyKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
