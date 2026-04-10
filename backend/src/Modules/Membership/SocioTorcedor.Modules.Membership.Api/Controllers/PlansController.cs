using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.Commands.CreateMemberPlan;
using SocioTorcedor.Modules.Membership.Application.Commands.ToggleMemberPlanStatus;
using SocioTorcedor.Modules.Membership.Application.Commands.UpdateMemberPlan;
using SocioTorcedor.Modules.Membership.Application.Queries.GetMemberPlanById;
using SocioTorcedor.Modules.Membership.Application.Queries.ListMemberPlans;

namespace SocioTorcedor.Modules.Membership.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PlansController(IMediator mediator) : ControllerBase
{
    public sealed class CreateMemberPlanBody
    {
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        public decimal Preco { get; set; }

        public IReadOnlyList<string>? Vantagens { get; set; }
    }

    public sealed class UpdateMemberPlanBody
    {
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        public decimal Preco { get; set; }

        public IReadOnlyList<string>? Vantagens { get; set; }
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMemberPlanBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateMemberPlanCommand(body.Nome, body.Descricao, body.Preco, body.Vantagens),
            cancellationToken);

        if (!result.IsSuccess)
            return MapError(result);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ListMemberPlansQuery(page, pageSize), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result);

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMemberPlanByIdQuery(id), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result);

        return Ok(result.Value);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMemberPlanBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateMemberPlanCommand(id, body.Nome, body.Descricao, body.Preco, body.Vantagens),
            cancellationToken);

        if (!result.IsSuccess)
            return MapError(result);

        return Ok(result.Value);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ToggleMemberPlanStatusCommand(id), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result);

        return Ok(result.Value);
    }

    private static IActionResult MapError<T>(Result<T> result) =>
        result.Error!.Code switch
        {
            "Membership.PlanNotFound" => new NotFoundObjectResult(new { code = result.Error.Code, message = result.Error.Message }),
            "Membership.PlanNameConflict" => new ConflictObjectResult(new { code = result.Error.Code, message = result.Error.Message }),
            "Membership.InvalidInput" => new BadRequestObjectResult(new { code = result.Error.Code, message = result.Error.Message }),
            "Tenant.Required" => new BadRequestObjectResult(new { code = result.Error.Code, message = result.Error.Message }),
            _ => new BadRequestObjectResult(new { code = result.Error.Code, message = result.Error.Message })
        };
}
