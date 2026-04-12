using FluentAssertions;
using Microsoft.Extensions.Options;
using MsOptions = Microsoft.Extensions.Options.Options;
using NSubstitute;
using SocioTorcedor.Modules.Payments.Application.Commands.ProcessMemberStripeWebhook;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Options;
using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Application.Tests.Commands;

public sealed class ProcessMemberStripeWebhookHandlerTests
{
    private static ProcessMemberStripeWebhookHandler CreateHandler(
        ITenantMasterPaymentsRepository repo,
        IMemberStripeWebhookEffectApplicator applicator) =>
        new(
            repo,
            applicator,
            MsOptions.Create(new StripeWebhookHandlingOptions()));

    [Fact]
    public async Task When_inbox_already_processed_returns_ok_without_reapplying()
    {
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        var processed = MemberStripeWebhookInbox.Receive("evt_1", "invoice.paid", "{}");
        processed.MarkProcessed();
        repo.GetMemberStripeWebhookByIdempotencyKeyAsync("evt_1", Arg.Any<CancellationToken>()).Returns(processed);

        var applicator = Substitute.For<IMemberStripeWebhookEffectApplicator>();
        var handler = CreateHandler(repo, applicator);

        var r = await handler.Handle(
            new ProcessMemberStripeWebhookCommand("evt_1", "invoice.paid", "{}"),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        await applicator.DidNotReceive().ApplyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_new_event_applies_effects_and_marks_processed()
    {
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetMemberStripeWebhookByIdempotencyKeyAsync("evt_s", Arg.Any<CancellationToken>())
            .Returns((MemberStripeWebhookInbox?)null);

        var applicator = Substitute.For<IMemberStripeWebhookEffectApplicator>();
        var handler = CreateHandler(repo, applicator);

        var r = await handler.Handle(
            new ProcessMemberStripeWebhookCommand("evt_s", "invoice.paid", "{}"),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        await applicator.Received(1).ApplyAsync("invoice.paid", "{}", Arg.Any<CancellationToken>());
        await repo.Received(1).AddMemberStripeWebhookAsync(Arg.Any<MemberStripeWebhookInbox>(), Arg.Any<CancellationToken>());
        await repo.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
