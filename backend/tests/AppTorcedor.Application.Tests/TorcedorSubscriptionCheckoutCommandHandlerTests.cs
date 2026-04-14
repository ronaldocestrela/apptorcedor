using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Torcedor.Commands.ConfirmSubscriptionPayment;
using AppTorcedor.Application.Modules.Torcedor.Commands.CreateSubscriptionCheckout;
using AppTorcedor.Identity;

namespace AppTorcedor.Application.Tests;

public sealed class TorcedorSubscriptionCheckoutCommandHandlerTests
{
    [Fact]
    public async Task Create_checkout_handler_delegates_to_port()
    {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var port = new FakeCheckoutPort
        {
            CreateResult = CreateTorcedorSubscriptionCheckoutResult.Success(
                Guid.NewGuid(),
                Guid.NewGuid(),
                10m,
                "BRL",
                TorcedorSubscriptionPaymentMethod.Pix,
                MembershipStatus.PendingPayment,
                new TorcedorSubscriptionCheckoutPixDto("qr", "copy"),
                null),
        };
        var handler = new CreateTorcedorSubscriptionCheckoutCommandHandler(port);

        var r = await handler.Handle(
            new CreateTorcedorSubscriptionCheckoutCommand(userId, planId, TorcedorSubscriptionPaymentMethod.Pix),
            CancellationToken.None);

        Assert.True(r.Ok);
        Assert.Single(port.CreateCalls);
        Assert.Equal(userId, port.CreateCalls[0].UserId);
        Assert.Equal(planId, port.CreateCalls[0].PlanId);
        Assert.Equal(TorcedorSubscriptionPaymentMethod.Pix, port.CreateCalls[0].Method);
    }

    [Fact]
    public async Task Confirm_payment_handler_delegates_to_port()
    {
        var paymentId = Guid.NewGuid();
        var port = new FakeCheckoutPort { ConfirmResult = ConfirmTorcedorSubscriptionPaymentResult.Success() };
        var handler = new ConfirmTorcedorSubscriptionPaymentCommandHandler(port);

        var r = await handler.Handle(new ConfirmTorcedorSubscriptionPaymentCommand(paymentId, "s"), CancellationToken.None);

        Assert.True(r.Ok);
        Assert.Single(port.ConfirmCalls);
        Assert.Equal(paymentId, port.ConfirmCalls[0].PaymentId);
        Assert.Equal("s", port.ConfirmCalls[0].Secret);
    }

    private sealed class FakeCheckoutPort : ITorcedorSubscriptionCheckoutPort
    {
        public List<(Guid UserId, Guid PlanId, TorcedorSubscriptionPaymentMethod Method)> CreateCalls { get; } = [];

        public List<(Guid PaymentId, string? Secret)> ConfirmCalls { get; } = [];

        public CreateTorcedorSubscriptionCheckoutResult CreateResult { get; init; } =
            CreateTorcedorSubscriptionCheckoutResult.SubscribeFailed(SubscribeMemberError.PlanNotFoundOrNotAvailable);

        public ConfirmTorcedorSubscriptionPaymentResult ConfirmResult { get; init; } =
            ConfirmTorcedorSubscriptionPaymentResult.Failure(ConfirmTorcedorSubscriptionPaymentError.NotFound);

        public Task<CreateTorcedorSubscriptionCheckoutResult> CreateCheckoutAsync(
            Guid userId,
            Guid planId,
            TorcedorSubscriptionPaymentMethod paymentMethod,
            CancellationToken cancellationToken = default)
        {
            CreateCalls.Add((userId, planId, paymentMethod));
            return Task.FromResult(CreateResult);
        }

        public Task<ConfirmTorcedorSubscriptionPaymentResult> ConfirmPaymentAsync(
            Guid paymentId,
            string? webhookSecret,
            CancellationToken cancellationToken = default)
        {
            ConfirmCalls.Add((paymentId, webhookSecret));
            return Task.FromResult(ConfirmResult);
        }

        public Task<ConfirmTorcedorSubscriptionPaymentResult> ConfirmPaymentAfterProviderSuccessAsync(
            Guid paymentId,
            string? providerPaymentReference,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ConfirmResult);
    }
}
