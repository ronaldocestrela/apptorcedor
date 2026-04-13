using System.Security.Claims;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Torcedor.Commands.CancelMySupportTicket;
using AppTorcedor.Application.Modules.Torcedor.Commands.CreateMySupportTicket;
using AppTorcedor.Application.Modules.Torcedor.Commands.ReopenMySupportTicket;
using AppTorcedor.Application.Modules.Torcedor.Commands.ReplyMySupportTicket;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetMySupportTicket;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetSupportAttachmentDownload;
using AppTorcedor.Application.Modules.Torcedor.Queries.ListMySupportTickets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/support/tickets")]
[Authorize]
[RequestSizeLimit(32 * 1024 * 1024)]
public sealed class TorcedorSupportTicketsController(
    IMediator mediator,
    ISupportTicketAttachmentStorage attachmentStorage,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TorcedorSupportTicketListPageResponse>> List(
        [FromQuery] SupportTicketStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var pageDto = await mediator
            .Send(new ListMySupportTicketsQuery(userId.Value, status, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        var items = pageDto.Items
            .Select(
                i => new TorcedorSupportTicketListItemResponse(
                    i.TicketId,
                    i.Queue,
                    i.Subject,
                    i.Priority,
                    i.Status,
                    i.SlaDeadlineUtc,
                    i.IsSlaBreached,
                    i.FirstResponseAtUtc,
                    i.CreatedAtUtc,
                    i.UpdatedAtUtc))
            .ToList();
        return Ok(new TorcedorSupportTicketListPageResponse(pageDto.TotalCount, items));
    }

    [HttpGet("{ticketId:guid}")]
    public async Task<ActionResult<TorcedorSupportTicketDetailResponse>> GetById(Guid ticketId, CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var d = await mediator.Send(new GetMySupportTicketQuery(userId.Value, ticketId), cancellationToken).ConfigureAwait(false);
        if (d is null)
            return NotFound();
        return Ok(MapDetail(d));
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromForm] CreateTorcedorSupportTicketForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var attachments = await ReadAttachmentsAsync(form.Attachments, cancellationToken).ConfigureAwait(false);
        if (attachments.Error is { } err)
            return BadRequest(new { error = err });

        var result = await mediator
            .Send(
                new CreateMySupportTicketCommand(
                    userId.Value,
                    form.Queue,
                    form.Subject,
                    form.Priority,
                    form.InitialMessage,
                    attachments.Items),
                cancellationToken)
            .ConfigureAwait(false);

        if (result.Error is { } e)
            return BadRequest(new { error = MapCreateError(e) });

        return CreatedAtAction(nameof(GetById), new { ticketId = result.TicketId }, new { ticketId = result.TicketId });
    }

    [HttpPost("{ticketId:guid}/reply")]
    public async Task<IActionResult> Reply(Guid ticketId, [FromForm] ReplyTorcedorSupportTicketForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var attachments = await ReadAttachmentsAsync(form.Attachments, cancellationToken).ConfigureAwait(false);
        if (attachments.Error is { } err)
            return BadRequest(new { error = err });

        var result = await mediator
            .Send(
                new ReplyMySupportTicketCommand(userId.Value, ticketId, form.Body ?? string.Empty, attachments.Items),
                cancellationToken)
            .ConfigureAwait(false);
        return MapMutation(result);
    }

    [HttpPost("{ticketId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid ticketId, CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var result = await mediator
            .Send(new CancelMySupportTicketCommand(userId.Value, ticketId, null), cancellationToken)
            .ConfigureAwait(false);
        return MapMutation(result);
    }

    [HttpPost("{ticketId:guid}/reopen")]
    public async Task<IActionResult> Reopen(Guid ticketId, CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var result = await mediator
            .Send(new ReopenMySupportTicketCommand(userId.Value, ticketId), cancellationToken)
            .ConfigureAwait(false);
        return MapMutation(result);
    }

    [HttpGet("{ticketId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var dto = await mediator
            .Send(new GetSupportAttachmentDownloadQuery(userId.Value, ticketId, attachmentId), cancellationToken)
            .ConfigureAwait(false);
        if (dto is null)
            return NotFound();

        var stream = attachmentStorage.OpenRead(dto.StorageKey);
        if (stream is null)
            return NotFound();

        return File(stream, dto.ContentType, dto.FileName);
    }

    private static TorcedorSupportTicketDetailResponse MapDetail(TorcedorSupportTicketDetailDto d)
    {
        var messages = d.Messages
            .Select(
                m => new TorcedorSupportMessageResponse(
                    m.MessageId,
                    m.AuthorUserId,
                    m.Body,
                    m.CreatedAtUtc,
                    m.Attachments
                        .Select(
                            a => new TorcedorSupportAttachmentResponse(
                                a.AttachmentId,
                                a.FileName,
                                a.ContentType,
                                a.DownloadPath))
                        .ToList()))
            .ToList();
        var history = d.History
            .Select(
                h => new TorcedorSupportHistoryResponse(
                    h.EntryId,
                    h.EventType,
                    h.FromValue,
                    h.ToValue,
                    h.ActorUserId,
                    h.Reason,
                    h.CreatedAtUtc))
            .ToList();
        return new TorcedorSupportTicketDetailResponse(
            d.TicketId,
            d.Queue,
            d.Subject,
            d.Priority,
            d.Status,
            d.SlaDeadlineUtc,
            d.IsSlaBreached,
            d.FirstResponseAtUtc,
            d.CreatedAtUtc,
            d.UpdatedAtUtc,
            messages,
            history);
    }

    private async Task<(IReadOnlyList<SupportTorcedorAttachmentInput> Items, string? Error)> ReadAttachmentsAsync(
        List<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
            return ([], null);

        var maxFiles = Math.Max(1, configuration.GetValue("SupportTicketAttachments:MaxFilesPerMessage", 5));
        if (files.Count > maxFiles)
            return ([], "too_many_attachments");

        var maxBytes = Math.Max(1, configuration.GetValue("SupportTicketAttachments:MaxBytesPerFile", 5 * 1024 * 1024));
        var list = new List<SupportTorcedorAttachmentInput>();
        foreach (var file in files)
        {
            if (file.Length == 0)
                continue;
            if (file.Length > maxBytes)
                return ([], "attachment_too_large");

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            list.Add(new SupportTorcedorAttachmentInput(ms.ToArray(), file.FileName, file.ContentType));
        }

        return (list, null);
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

    private Guid? GetUserIdOrDefault()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var g) ? g : null;
    }
}
