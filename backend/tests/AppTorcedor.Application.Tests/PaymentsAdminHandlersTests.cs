using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CancelPayment;
using AppTorcedor.Application.Modules.Administration.Commands.ConciliatePayment;
using AppTorcedor.Application.Modules.Administration.Commands.RefundPayment;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminPaymentDetail;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminPayments;

namespace AppTorcedor.Application.Tests;

public sealed class PaymentsAdminHandlersTests
{
    [Fact]
    public async Task ListAdminPayments_delegates_to_port()
    {
        var fake = new FakePaymentsPort();
        var handler = new ListAdminPaymentsQueryHandler(fake);
        var uid = Guid.NewGuid();
        var page = await handler.Handle(
            new ListAdminPaymentsQuery("Pending", uid, null, "Pix", null, null, 2, 15),
            CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
        Assert.Equal("Pending", fake.ListCalls[0].Status);
        Assert.Equal(uid, fake.ListCalls[0].UserId);
        Assert.Equal("Pix", fake.ListCalls[0].PaymentMethod);
        Assert.Equal(2, fake.ListCalls[0].Page);
        Assert.Equal(15, fake.ListCalls[0].PageSize);
    }

    [Fact]
    public async Task GetAdminPaymentDetail_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakePaymentsPort();
        var handler = new GetAdminPaymentDetailQueryHandler(fake);
        var detail = await handler.Handle(new GetAdminPaymentDetailQuery(id), CancellationToken.None);
        Assert.Null(detail);
        Assert.Single(fake.DetailCalls);
        Assert.Equal(id, fake.DetailCalls[0]);
    }

    [Fact]
    public async Task ConciliatePayment_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var fake = new FakePaymentsPort { MutationResult = new PaymentMutationResult(true, null) };
        var handler = new ConciliatePaymentCommandHandler(fake);
        var r = await handler.Handle(new ConciliatePaymentCommand(id, null, actor), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.ConciliateCalls);
        Assert.Equal(id, fake.ConciliateCalls[0].PaymentId);
        Assert.Equal(actor, fake.ConciliateCalls[0].ActorUserId);
    }

    [Fact]
    public async Task CancelPayment_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var fake = new FakePaymentsPort { MutationResult = new PaymentMutationResult(true, null) };
        var handler = new CancelPaymentCommandHandler(fake);
        var r = await handler.Handle(new CancelPaymentCommand(id, "ops", actor), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.CancelCalls);
    }

    [Fact]
    public async Task RefundPayment_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var fake = new FakePaymentsPort { MutationResult = new PaymentMutationResult(true, null) };
        var handler = new RefundPaymentCommandHandler(fake);
        var r = await handler.Handle(new RefundPaymentCommand(id, "chargeback", actor), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.RefundCalls);
    }

    private sealed class FakePaymentsPort : IPaymentsAdministrationPort
    {
        public PaymentMutationResult MutationResult { get; init; } = new(false, PaymentMutationError.NotFound);

        public List<(string? Status, Guid? UserId, Guid? MembershipId, string? PaymentMethod, DateTimeOffset? DueFrom, DateTimeOffset? DueTo, int Page, int PageSize)> ListCalls { get; } = [];
        public List<Guid> DetailCalls { get; } = [];
        public List<(Guid PaymentId, DateTimeOffset? PaidAt, Guid ActorUserId)> ConciliateCalls { get; } = [];
        public List<(Guid PaymentId, string? Reason, Guid ActorUserId)> CancelCalls { get; } = [];
        public List<(Guid PaymentId, string? Reason, Guid ActorUserId)> RefundCalls { get; } = [];

        public Task<AdminPaymentListPageDto> ListPaymentsAsync(
            string? status,
            Guid? userId,
            Guid? membershipId,
            string? paymentMethod,
            DateTimeOffset? dueFrom,
            DateTimeOffset? dueTo,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((status, userId, membershipId, paymentMethod, dueFrom, dueTo, page, pageSize));
            return Task.FromResult(new AdminPaymentListPageDto(0, []));
        }

        public Task<AdminPaymentDetailDto?> GetPaymentByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
        {
            DetailCalls.Add(paymentId);
            return Task.FromResult<AdminPaymentDetailDto?>(null);
        }

        public Task<PaymentMutationResult> ConciliatePaymentAsync(
            Guid paymentId,
            DateTimeOffset? paidAt,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            ConciliateCalls.Add((paymentId, paidAt, actorUserId));
            return Task.FromResult(MutationResult);
        }

        public Task<PaymentMutationResult> CancelPaymentAsync(
            Guid paymentId,
            string? reason,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            CancelCalls.Add((paymentId, reason, actorUserId));
            return Task.FromResult(MutationResult);
        }

        public Task<PaymentMutationResult> RefundPaymentAsync(
            Guid paymentId,
            string? reason,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            RefundCalls.Add((paymentId, reason, actorUserId));
            return Task.FromResult(MutationResult);
        }
    }
}
