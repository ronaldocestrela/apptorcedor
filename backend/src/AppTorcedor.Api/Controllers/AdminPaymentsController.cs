using System.Security.Claims;
using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CancelPayment;
using AppTorcedor.Application.Modules.Administration.Commands.ConciliatePayment;
using AppTorcedor.Application.Modules.Administration.Commands.RefundPayment;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminPaymentDetail;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminPayments;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/payments")]
[Authorize]
public sealed class AdminPaymentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.PagamentosVisualizar)]
    public async Task<ActionResult<AdminPaymentListPageDto>> List(
        [FromQuery] string? status,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? membershipId,
        [FromQuery] string? paymentMethod,
        [FromQuery] DateTimeOffset? dueFrom,
        [FromQuery] DateTimeOffset? dueTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pageDto = await mediator
            .Send(
                new ListAdminPaymentsQuery(status, userId, membershipId, paymentMethod, dueFrom, dueTo, page, pageSize),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(pageDto);
    }

    [HttpGet("{paymentId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.PagamentosVisualizar)]
    public async Task<ActionResult<AdminPaymentDetailDto>> GetById(Guid paymentId, CancellationToken cancellationToken)
    {
        var detail = await mediator.Send(new GetAdminPaymentDetailQuery(paymentId), cancellationToken).ConfigureAwait(false);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("{paymentId:guid}/conciliate")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.PagamentosGerenciar)]
    public async Task<IActionResult> Conciliate(
        Guid paymentId,
        [FromBody] PaymentConciliateRequest? body,
        CancellationToken cancellationToken)
    {
        var actor = GetUserIdOrThrow();
        var result = await mediator
            .Send(new ConciliatePaymentCommand(paymentId, body?.PaidAt, actor), cancellationToken)
            .ConfigureAwait(false);
        return MapMutation(result);
    }

    [HttpPost("{paymentId:guid}/cancel")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.PagamentosGerenciar)]
    public async Task<IActionResult> Cancel(
        Guid paymentId,
        [FromBody] PaymentCancelRequest? body,
        CancellationToken cancellationToken)
    {
        var actor = GetUserIdOrThrow();
        var result = await mediator
            .Send(new CancelPaymentCommand(paymentId, body?.Reason, actor), cancellationToken)
            .ConfigureAwait(false);
        return MapMutation(result);
    }

    [HttpPost("{paymentId:guid}/refund")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.PagamentosEstornar)]
    public async Task<IActionResult> Refund(
        Guid paymentId,
        [FromBody] PaymentRefundRequest? body,
        CancellationToken cancellationToken)
    {
        var actor = GetUserIdOrThrow();
        var result = await mediator
            .Send(new RefundPaymentCommand(paymentId, body?.Reason, actor), cancellationToken)
            .ConfigureAwait(false);
        return MapMutation(result);
    }

    private IActionResult MapMutation(PaymentMutationResult result)
    {
        if (result.Ok)
            return NoContent();
        return result.Error switch
        {
            PaymentMutationError.NotFound => NotFound(),
            PaymentMutationError.InvalidTransition => BadRequest(new { error = "Invalid payment status transition." }),
            PaymentMutationError.Conflict => Conflict(new { error = "Conflict." }),
            _ => BadRequest(),
        };
    }

    private Guid GetUserIdOrThrow()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
            throw new InvalidOperationException("Authenticated user id is missing or invalid.");
        return guid;
    }
}
