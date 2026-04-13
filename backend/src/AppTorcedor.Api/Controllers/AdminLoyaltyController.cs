using System.Security.Claims;
using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CreateLoyaltyCampaign;
using AppTorcedor.Application.Modules.Administration.Commands.ManualLoyaltyPointsAdjustment;
using AppTorcedor.Application.Modules.Administration.Commands.PublishLoyaltyCampaign;
using AppTorcedor.Application.Modules.Administration.Commands.UnpublishLoyaltyCampaign;
using AppTorcedor.Application.Modules.Administration.Commands.UpdateLoyaltyCampaign;
using AppTorcedor.Application.Modules.Administration.Queries.GetLoyaltyAllTimeRanking;
using AppTorcedor.Application.Modules.Administration.Queries.GetLoyaltyCampaign;
using AppTorcedor.Application.Modules.Administration.Queries.GetLoyaltyMonthlyRanking;
using AppTorcedor.Application.Modules.Administration.Queries.ListLoyaltyCampaigns;
using AppTorcedor.Application.Modules.Administration.Queries.ListLoyaltyUserLedger;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/loyalty")]
[Authorize]
public sealed class AdminLoyaltyController(IMediator mediator) : ControllerBase
{
    [HttpGet("campaigns")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.FidelidadeVisualizar)]
    public async Task<ActionResult<LoyaltyCampaignListPageDto>> ListCampaigns(
        [FromQuery] LoyaltyCampaignStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var dto = await mediator.Send(new ListLoyaltyCampaignsQuery(status, page, pageSize), cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("campaigns/{campaignId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.FidelidadeVisualizar)]
    public async Task<ActionResult<LoyaltyCampaignDetailDto>> GetCampaign(Guid campaignId, CancellationToken cancellationToken)
    {
        var d = await mediator.Send(new GetLoyaltyCampaignQuery(campaignId), cancellationToken).ConfigureAwait(false);
        return d is null ? NotFound() : Ok(d);
    }

    [HttpPost("campaigns")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.FidelidadeGerenciar)]
    public async Task<ActionResult<object>> CreateCampaign([FromBody] UpsertLoyaltyCampaignRequest body, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var dto = MapWrite(body);
        var result = await mediator.Send(new CreateLoyaltyCampaignCommand(dto), cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
            return BadRequest(new { error = "Validation failed." });

        return CreatedAtAction(nameof(GetCampaign), new { campaignId = result.CampaignId }, new { campaignId = result.CampaignId });
    }

    [HttpPut("campaigns/{campaignId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.FidelidadeGerenciar)]
    public async Task<IActionResult> UpdateCampaign(
        Guid campaignId,
        [FromBody] UpsertLoyaltyCampaignRequest body,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await mediator
            .Send(new UpdateLoyaltyCampaignCommand(campaignId, MapWrite(body)), cancellationToken)
            .ConfigureAwait(false);
        return MapLoyaltyMutation(result);
    }

    [HttpPost("campaigns/{campaignId:guid}/publish")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.FidelidadeGerenciar)]
    public async Task<IActionResult> PublishCampaign(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PublishLoyaltyCampaignCommand(campaignId), cancellationToken).ConfigureAwait(false);
        return MapLoyaltyMutation(result);
    }

    [HttpPost("campaigns/{campaignId:guid}/unpublish")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.FidelidadeGerenciar)]
    public async Task<IActionResult> UnpublishCampaign(Guid campaignId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UnpublishLoyaltyCampaignCommand(campaignId), cancellationToken).ConfigureAwait(false);
        return MapLoyaltyMutation(result);
    }

    [HttpPost("users/{userId:guid}/manual-adjustments")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.FidelidadeGerenciar)]
    public async Task<IActionResult> ManualAdjust(
        Guid userId,
        [FromBody] ManualLoyaltyPointsRequest body,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var actor = GetUserIdOrThrow();
        var result = await mediator
            .Send(
                new ManualLoyaltyPointsAdjustmentCommand(userId, body.Points, body.Reason, body.CampaignId, actor),
                cancellationToken)
            .ConfigureAwait(false);

        if (result.Ok)
            return NoContent();

        return result.Error switch
        {
            LoyaltyManualAdjustError.NotFound => NotFound(),
            LoyaltyManualAdjustError.Validation => BadRequest(new { error = "Validation failed." }),
            _ => BadRequest(),
        };
    }

    [HttpGet("users/{userId:guid}/ledger")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.FidelidadeVisualizar)]
    public async Task<ActionResult<LoyaltyLedgerPageDto>> UserLedger(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var dto = await mediator.Send(new ListLoyaltyUserLedgerQuery(userId, page, pageSize), cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("rankings/monthly")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.FidelidadeVisualizar)]
    public async Task<ActionResult<LoyaltyRankingPageDto>> MonthlyRanking(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var dto = await mediator
            .Send(new GetLoyaltyMonthlyRankingQuery(year, month, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("rankings/all-time")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.FidelidadeVisualizar)]
    public async Task<ActionResult<LoyaltyRankingPageDto>> AllTimeRanking(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var dto = await mediator
            .Send(new GetLoyaltyAllTimeRankingQuery(page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    private static LoyaltyCampaignWriteDto MapWrite(UpsertLoyaltyCampaignRequest body) =>
        new(
            body.Name,
            body.Description,
            body.Rules.Select(r => new LoyaltyPointRuleWriteDto(r.Trigger, r.Points, r.SortOrder)).ToList());

    private IActionResult MapLoyaltyMutation(LoyaltyMutationResult result)
    {
        if (result.Ok)
            return NoContent();
        return result.Error switch
        {
            LoyaltyMutationError.NotFound => NotFound(),
            LoyaltyMutationError.Validation => BadRequest(new { error = "Validation failed." }),
            LoyaltyMutationError.InvalidState => BadRequest(new { error = "Invalid state transition." }),
            _ => BadRequest(),
        };
    }

    private Guid GetUserIdOrThrow()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var g))
            throw new InvalidOperationException("Authenticated user id is missing.");
        return g;
    }
}
