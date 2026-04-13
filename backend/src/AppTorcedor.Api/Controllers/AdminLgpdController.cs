using System.Security.Claims;
using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Modules.Lgpd.Commands.AddLegalDocumentVersion;
using AppTorcedor.Application.Modules.Lgpd.Commands.AnonymizeUser;
using AppTorcedor.Application.Modules.Lgpd.Commands.CreateLegalDocument;
using AppTorcedor.Application.Modules.Lgpd.Commands.ExportUserData;
using AppTorcedor.Application.Modules.Lgpd.Commands.PublishLegalDocumentVersion;
using AppTorcedor.Application.Modules.Lgpd.Commands.RecordUserConsent;
using AppTorcedor.Application.Modules.Lgpd.Queries.GetLegalDocument;
using AppTorcedor.Application.Modules.Lgpd.Queries.ListLegalDocuments;
using AppTorcedor.Application.Modules.Lgpd.Queries.ListUserConsents;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/lgpd")]
[Authorize]
public sealed class AdminLgpdController(IMediator mediator) : ControllerBase
{
    [HttpGet("documents")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.LgpdDocumentosVisualizar)]
    public async Task<IActionResult> ListDocuments(CancellationToken cancellationToken)
    {
        var rows = await mediator.Send(new ListLegalDocumentsQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpGet("documents/{id:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.LgpdDocumentosVisualizar)]
    public async Task<IActionResult> GetDocument(Guid id, CancellationToken cancellationToken)
    {
        var doc = await mediator.Send(new GetLegalDocumentQuery(id), cancellationToken).ConfigureAwait(false);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpPost("documents")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.LgpdDocumentosEditar)]
    public async Task<IActionResult> CreateDocument([FromBody] CreateLegalDocumentRequest body, CancellationToken cancellationToken)
    {
        try
        {
            var created = await mediator
                .Send(new CreateLegalDocumentCommand(body.Type, body.Title), cancellationToken)
                .ConfigureAwait(false);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("documents/{documentId:guid}/versions")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.LgpdDocumentosEditar)]
    public async Task<IActionResult> AddVersion(Guid documentId, [FromBody] AddLegalDocumentVersionRequest body, CancellationToken cancellationToken)
    {
        try
        {
            var v = await mediator
                .Send(new AddLegalDocumentVersionCommand(documentId, body.Content), cancellationToken)
                .ConfigureAwait(false);
            return Ok(v);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("legal-document-versions/{versionId:guid}/publish")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.LgpdDocumentosEditar)]
    public async Task<IActionResult> PublishVersion(Guid versionId, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Send(new PublishLegalDocumentVersionCommand(versionId), cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("users/{userId:guid}/consents")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.LgpdConsentimentosVisualizar)]
    public async Task<IActionResult> ListConsents(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await mediator.Send(new ListUserConsentsQuery(userId), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpPost("users/{userId:guid}/consents")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.LgpdConsentimentosRegistrar)]
    public async Task<IActionResult> RecordConsent(Guid userId, [FromBody] RecordUserConsentRequest body, CancellationToken cancellationToken)
    {
        try
        {
            await mediator
                .Send(new RecordUserConsentCommand(userId, body.DocumentVersionId, body.ClientIp), cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("users/{userId:guid}/export")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.LgpdDadosExportar)]
    public async Task<IActionResult> ExportUser(Guid userId, CancellationToken cancellationToken)
    {
        var actor = GetUserIdOrThrow();
        var result = await mediator.Send(new ExportUserDataCommand(userId, actor), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("users/{userId:guid}/anonymize")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.LgpdDadosAnonimizar)]
    public async Task<IActionResult> AnonymizeUser(Guid userId, CancellationToken cancellationToken)
    {
        var actor = GetUserIdOrThrow();
        var result = await mediator.Send(new AnonymizeUserCommand(userId, actor), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    private Guid GetUserIdOrThrow()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
            throw new InvalidOperationException("Missing user id.");
        return guid;
    }
}
