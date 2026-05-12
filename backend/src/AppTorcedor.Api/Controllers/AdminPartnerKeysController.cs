using System.Security.Claims;
using AppTorcedor.Api.Authorization;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CreatePartnerApiKey;
using AppTorcedor.Application.Modules.Administration.Commands.RevokePartnerApiKey;
using AppTorcedor.Application.Modules.Administration.Queries.ListPartnerApiKeys;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/partner-keys")]
[Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.IntegracoesGerenciar)]
public sealed class AdminPartnerKeysController(IMediator mediator) : ControllerBase
{
    /// <summary>Lista todas as API keys de parceiros (sem hash, sem chave em texto claro).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PartnerApiKeyListItemDto>>> List(CancellationToken cancellationToken)
    {
        var items = await mediator.Send(new ListPartnerApiKeysQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(items);
    }

    /// <summary>
    /// Cria uma nova API key para um parceiro.
    /// A chave em texto claro (<c>plaintextKey</c>) é retornada APENAS nesta resposta — armazene-a imediatamente.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PartnerApiKeyCreatedDto>> Create(
        [FromBody] CreatePartnerApiKeyRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new { error = "Name is required." });

        var callerUserId = GetUserIdOrDefault();
        var dto = await mediator
            .Send(new CreatePartnerApiKeyCommand(body.Name, callerUserId), cancellationToken)
            .ConfigureAwait(false);

        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>Revoga (desativa) uma API key de parceiro. A key deixa de funcionar imediatamente.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        var found = await mediator.Send(new RevokePartnerApiKeyCommand(id), cancellationToken).ConfigureAwait(false);
        return found ? NoContent() : NotFound();
    }

    private Guid? GetUserIdOrDefault()
    {
        var value = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}

/// <param name="Name">Nome descritivo do parceiro (ex.: "Loja XYZ").</param>
public sealed record CreatePartnerApiKeyRequest(string Name);
