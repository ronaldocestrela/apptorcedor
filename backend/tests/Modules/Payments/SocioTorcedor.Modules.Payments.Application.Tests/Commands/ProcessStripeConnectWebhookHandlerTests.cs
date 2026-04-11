using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Payments.Application.Commands.ProcessStripeConnectWebhook;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using StripeWebhookHandlingOptions = SocioTorcedor.Modules.Payments.Application.Options.StripeWebhookHandlingOptions;

namespace SocioTorcedor.Modules.Payments.Application.Tests.Commands;

public sealed class ProcessStripeConnectWebhookHandlerTests
{
    private static ProcessStripeConnectWebhookHandler CreateHandler(
        ITenantMasterPaymentsRepository repo,
        IConnectStripeWebhookEffectApplicator applicator,
        StripeWebhookHandlingOptions? opts = null) =>
        new(repo, applicator, Microsoft.Extensions.Options.Options.Create(opts ?? new StripeWebhookHandlingOptions()));

    [Fact]
    public async Task Processed_inbox_returns_ok_without_calling_applicator()
    {
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        var applicator = Substitute.For<IConnectStripeWebhookEffectApplicator>();
        var processed = ConnectStripeWebhookInbox.Receive("evt_1", "account.updated", "{}");
        processed.MarkProcessed();
        repo.GetConnectWebhookByIdempotencyKeyAsync("evt_1", Arg.Any<CancellationToken>()).Returns(processed);

        var handler = CreateHandler(repo, applicator);
        var body = """{"data":{"object":{"id":"acct_1"}}}""";
        var r = await handler.Handle(
            new ProcessStripeConnectWebhookCommand("evt_1", "account.updated", body),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        await applicator.DidNotReceive()
            .ApplyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Shadow_mode_skips_inbox_but_invokes_applicator_for_logging()
    {
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        var applicator = Substitute.For<IConnectStripeWebhookEffectApplicator>();
        var handler = CreateHandler(
            repo,
            applicator,
            new StripeWebhookHandlingOptions { StripeWebhookShadowMode = true });

        var r = await handler.Handle(
            new ProcessStripeConnectWebhookCommand("evt_s", "invoice.paid", "{}"),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        await repo.DidNotReceive()
            .GetConnectWebhookByIdempotencyKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await applicator.Received(1).ApplyAsync("invoice.paid", "{}", Arg.Any<CancellationToken>());
    }
}
