using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CreateAdminGame;
using AppTorcedor.Application.Modules.Administration.Commands.DeactivateAdminGame;
using AppTorcedor.Application.Modules.Administration.Commands.UpdateAdminGame;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminGame;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminGames;
using AppTorcedor.Application.Modules.Games.Commands.UploadOpponentLogo;
using AppTorcedor.Application.Modules.Games.Queries.ListOpponentLogoAssets;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/games")]
[Authorize]
public sealed class AdminGamesController(IMediator mediator) : ControllerBase
{
    [HttpGet("opponent-logos")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.JogosVisualizar)]
    public async Task<ActionResult<OpponentLogoAssetListPageResponse>> ListOpponentLogos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var pageDto = await mediator
            .Send(new ListOpponentLogoAssetsQuery(page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        var items = pageDto.Items
            .Select(i => new OpponentLogoAssetListItemResponse(i.Id, i.Url, i.CreatedAt))
            .ToList();
        return Ok(new OpponentLogoAssetListPageResponse(pageDto.TotalCount, items));
    }

    [HttpPost("opponent-logos")]
    [Authorize(Policy = Policies.GamesOpponentLogosUpload)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<OpponentLogoUploadResponse>> UploadOpponentLogo(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "File is required." });

        await using var stream = file.OpenReadStream();
        var result = await mediator
            .Send(new UploadOpponentLogoCommand(stream, file.FileName, file.ContentType ?? string.Empty), cancellationToken)
            .ConfigureAwait(false);
        if (result is null)
            return BadRequest(new { error = "Unable to upload opponent logo." });

        return Ok(new OpponentLogoUploadResponse(result.Url));
    }

    [HttpGet]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.JogosVisualizar)]
    public async Task<ActionResult<AdminGameListPageDto>> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pageDto = await mediator
            .Send(new ListAdminGamesQuery(search, isActive, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(pageDto);
    }

    [HttpGet("{gameId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.JogosVisualizar)]
    public async Task<ActionResult<AdminGameDetailDto>> GetById(Guid gameId, CancellationToken cancellationToken)
    {
        var detail = await mediator.Send(new GetAdminGameQuery(gameId), cancellationToken).ConfigureAwait(false);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.JogosCriar)]
    public async Task<ActionResult<object>> Create([FromBody] UpsertGameRequest body, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var dto = Map(body);
        var result = await mediator.Send(new CreateAdminGameCommand(dto), cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
            return BadRequest(new { error = MapCreateError(result.Error) });

        return CreatedAtAction(nameof(GetById), new { gameId = result.GameId }, new { gameId = result.GameId });
    }

    [HttpPut("{gameId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.JogosEditar)]
    public async Task<IActionResult> Update(Guid gameId, [FromBody] UpsertGameRequest body, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await mediator
            .Send(new UpdateAdminGameCommand(gameId, Map(body)), cancellationToken)
            .ConfigureAwait(false);
        return MapGameMutation(result);
    }

    [HttpDelete("{gameId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.JogosEditar)]
    public async Task<IActionResult> Deactivate(Guid gameId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeactivateAdminGameCommand(gameId), cancellationToken).ConfigureAwait(false);
        return MapGameMutation(result);
    }

    private static AdminGameWriteDto Map(UpsertGameRequest body) =>
        new(body.Opponent, body.Competition, body.GameDate, body.IsActive, body.OpponentLogoUrl);

    private static string MapCreateError(GameMutationError? error) =>
        error switch
        {
            GameMutationError.Validation => "Validation failed.",
            _ => "Unable to create game.",
        };

    private IActionResult MapGameMutation(GameMutationResult result)
    {
        if (result.Ok)
            return NoContent();
        return result.Error switch
        {
            GameMutationError.NotFound => NotFound(),
            GameMutationError.Validation => BadRequest(new { error = "Validation failed." }),
            _ => BadRequest(),
        };
    }
}
