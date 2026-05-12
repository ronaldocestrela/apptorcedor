using AppTorcedor.Api.Auth;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Partner.Queries.LookupByPhone;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

/// <summary>
/// Endpoint de integração para plataformas parceiras autenticadas via API Key (<c>X-Api-Key</c>).
/// Nunca expõe dados pessoais além do resultado booleano (conformidade LGPD).
/// </summary>
[ApiController]
[Route("api/partner/v1")]
[Authorize(AuthenticationSchemes = PartnerApiKeyAuthHandler.SchemeName)]
public sealed class PartnerController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Verifica se um número de telefone pertence a um usuário cadastrado e se este é sócio ativo.
    /// </summary>
    /// <param name="phone">
    /// Número de telefone a consultar. Aceita qualquer formatação; somente dígitos são considerados.
    /// Exemplo: <c>11999999999</c> ou <c>+55 (11) 99999-9999</c>.
    /// </param>
    /// <returns>
    /// <c>exists</c>: indica se há usuário com esse telefone cadastrado.<br/>
    /// <c>isActiveMember</c>: indica se o usuário é sócio ativo no momento da consulta.
    /// </returns>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(PartnerLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PartnerLookupResponse>> Lookup(
        [FromQuery] string? phone,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { error = "O parâmetro 'phone' é obrigatório." });

        var result = await mediator
            .Send(new LookupPartnerByPhoneQuery(phone), cancellationToken)
            .ConfigureAwait(false);

        return Ok(new PartnerLookupResponse(result.Exists, result.IsActiveMember));
    }
}

/// <param name="Exists">Indica se há usuário cadastrado com esse telefone.</param>
/// <param name="IsActiveMember">Indica se o usuário possui associação ativa no momento da consulta.</param>
public sealed record PartnerLookupResponse(bool Exists, bool IsActiveMember);
