using System.Security.Claims;
using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.AssignAdminSupportTicket;
using AppTorcedor.Application.Modules.Administration.Commands.ChangeAdminSupportTicketStatus;
using AppTorcedor.Application.Modules.Administration.Commands.CreateAdminSupportTicket;
using AppTorcedor.Application.Modules.Administration.Commands.ReplyAdminSupportTicket;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminSupportAttachmentDownload;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminSupportTicket;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminSupportTickets;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/support/tickets")]
[Authorize]
public sealed class AdminSupportTicketsController(IMediator mediator, ISupportTicketAttachmentStorage attachmentStorage)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ChamadosResponder)]
    public async Task<ActionResult<AdminSupportTicketListPageDto>> List(
        [FromQuery] string? queue,
        [FromQuery] SupportTicketStatus? status,
        [FromQuery] Guid? assignedUserId,
        [FromQuery] bool? unassignedOnly,
        [FromQuery] bool? slaBreachedOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pageDto = await mediator
            .Send(
                new ListAdminSupportTicketsQuery(
                    queue,
                    status,
                    assignedUserId,
                    unassignedOnly,
                    slaBreachedOnly,
                    page,
                    pageSize),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(pageDto);
    }

    [HttpGet("{ticketId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ChamadosResponder)]
    public async Task<ActionResult<AdminSupportTicketDetailDto>> GetById(Guid ticketId, CancellationToken cancellationToken)
    {
        var detail = await mediator.Send(new GetAdminSupportTicketQuery(ticketId), cancellationToken).ConfigureAwait(false);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ChamadosResponder)]
    public async Task<ActionResult<object>> Create([FromBody] CreateSupportTicketRequest body, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var actor = GetUserIdOrThrow();
        var dto = new AdminSupportTicketCreateDto(
            body.RequesterUserId,
            body.Queue,
            body.Subject,
            body.Priority,
            body.InitialMessage);

        var result = await mediator.Send(new CreateAdminSupportTicketCommand(dto, actor), cancellationToken).ConfigureAwait(false);
        if (result.Error is { } err)
            return BadRequest(new { error = MapCreateError(err) });

        return CreatedAtAction(nameof(GetById), new { ticketId = result.TicketId }, new { ticketId = result.TicketId });
    }

    [HttpPost("{ticketId:guid}/reply")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ChamadosResponder)]
    public async Task<IActionResult> Reply(Guid ticketId, [FromBody] ReplySupportTicketRequest body, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var actor = GetUserIdOrThrow();
        var result = await mediator
            .Send(new ReplyAdminSupportTicketCommand(ticketId, body.Body, body.IsInternal, actor), cancellationToken)
            .ConfigureAwait(false);
        return MapMutation(result);
    }

    [HttpPost("{ticketId:guid}/assign")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ChamadosResponder)]
    public async Task<IActionResult> Assign(Guid ticketId, [FromBody] AssignSupportTicketRequest body, CancellationToken cancellationToken)
    {
        var actor = GetUserIdOrThrow();
        var result = await mediator
            .Send(new AssignAdminSupportTicketCommand(ticketId, body.AgentUserId, actor), cancellationToken)
            .ConfigureAwait(false);
        return MapMutation(result);
    }

    [HttpGet("{ticketId:guid}/attachments/{attachmentId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ChamadosResponder)]
    public async Task<IActionResult> DownloadAttachment(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var dto = await mediator
            .Send(new GetAdminSupportAttachmentDownloadQuery(ticketId, attachmentId), cancellationToken)
            .ConfigureAwait(false);
        if (dto is null)
            return NotFound();

        var stream = await attachmentStorage.OpenReadAsync(dto.StorageKey, cancellationToken).ConfigureAwait(false);
        if (stream is null)
            return NotFound();

        return File(stream, dto.ContentType, dto.FileName);
    }

    [HttpPost("{ticketId:guid}/status")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ChamadosResponder)]
    public async Task<IActionResult> ChangeStatus(
        Guid ticketId,
        [FromBody] ChangeSupportTicketStatusRequest body,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var actor = GetUserIdOrThrow();
        var result = await mediator
            .Send(new ChangeAdminSupportTicketStatusCommand(ticketId, body.Status, body.Reason, actor), cancellationToken)
            .ConfigureAwait(false);
        return MapMutation(result);
    }

    private static string MapCreateError(SupportTicketMutationError err) =>
        err switch
        {
            SupportTicketMutationError.Validation => "validation_failed",
            _ => "create_failed",
        };

    private IActionResult MapMutation(SupportTicketMutationResult result)
    {
        if (result.Ok)
            return NoContent();

        return result.Error switch
        {
            SupportTicketMutationError.NotFound => NotFound(),
            SupportTicketMutationError.Validation => BadRequest(new { error = "validation_failed" }),
            SupportTicketMutationError.InvalidStatusTransition => BadRequest(new { error = "invalid_status_transition" }),
            SupportTicketMutationError.Conflict => Conflict(new { error = "conflict" }),
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
