using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.Commands.CreateMemberProfile;
using SocioTorcedor.Modules.Membership.Application.Commands.UpdateMemberProfile;
using SocioTorcedor.Modules.Membership.Application.Queries.GetMemberById;
using SocioTorcedor.Modules.Membership.Application.Queries.GetMyProfile;
using SocioTorcedor.Modules.Membership.Application.Queries.ListMembers;
using SocioTorcedor.Modules.Membership.Domain.Enums;

namespace SocioTorcedor.Modules.Membership.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MembersController(IMediator mediator) : ControllerBase
{
    public sealed class CreateMemberProfileBody
    {
        public string Cpf { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public Gender Gender { get; set; }

        public string Phone { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;

        public string Number { get; set; } = string.Empty;

        public string? Complement { get; set; }

        public string Neighborhood { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string ZipCode { get; set; } = string.Empty;
    }

    public sealed class UpdateMemberProfileBody
    {
        public DateTime DateOfBirth { get; set; }

        public Gender Gender { get; set; }

        public string Phone { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;

        public string Number { get; set; } = string.Empty;

        public string? Complement { get; set; }

        public string Neighborhood { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string ZipCode { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMemberProfileBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateMemberProfileCommand(
                body.Cpf,
                body.DateOfBirth,
                body.Gender,
                body.Phone,
                body.Street,
                body.Number,
                body.Complement,
                body.Neighborhood,
                body.City,
                body.State,
                body.ZipCode),
            cancellationToken);

        if (!result.IsSuccess)
            return MapError(result);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyProfileQuery(), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result);

        return Ok(result.Value);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMine([FromBody] UpdateMemberProfileBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateMemberProfileCommand(
                body.DateOfBirth,
                body.Gender,
                body.Phone,
                body.Street,
                body.Number,
                body.Complement,
                body.Neighborhood,
                body.City,
                body.State,
                body.ZipCode),
            cancellationToken);

        if (!result.IsSuccess)
            return MapError(result);

        return Ok(result.Value);
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMemberByIdQuery(id), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result);

        return Ok(result.Value);
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ListMembersQuery(page, pageSize), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result);

        return Ok(result.Value);
    }

    private static IActionResult MapError<T>(Result<T> result) =>
        result.Error!.Code switch
        {
            "Membership.ProfileNotFound" => new NotFoundObjectResult(new { code = result.Error.Code, message = result.Error.Message }),
            "Membership.CpfConflict" or "Membership.ProfileExists" => new ConflictObjectResult(new { code = result.Error.Code, message = result.Error.Message }),
            "Membership.InvalidInput" => new BadRequestObjectResult(new { code = result.Error.Code, message = result.Error.Message }),
            "Tenant.Required" => new BadRequestObjectResult(new { code = result.Error.Code, message = result.Error.Message }),
            "Membership.UserRequired" => new UnauthorizedObjectResult(new { code = result.Error.Code, message = result.Error.Message }),
            _ => new BadRequestObjectResult(new { code = result.Error.Code, message = result.Error.Message })
        };
}
