using System.Security.Claims;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Api.Services;
using AppTorcedor.Application.Modules.Account;
using AppTorcedor.Application.Modules.Account.Commands.RegisterTorcedor;
using AppTorcedor.Application.Modules.Account.Commands.UpsertMyProfile;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account.Queries.GetMyDigitalCard;
using AppTorcedor.Application.Modules.Account.Queries.GetMyProfile;
using AppTorcedor.Application.Modules.Account.Queries.GetRegistrationLegalRequirements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController(
    IMediator mediator,
    IAuthService auth,
    IProfilePhotoStorage photoStorage) : ControllerBase
{
    [HttpGet("register/requirements")]
    [AllowAnonymous]
    public async Task<ActionResult<RegistrationLegalRequirementsResponse>> RegisterRequirements(CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetRegistrationLegalRequirementsQuery(), cancellationToken).ConfigureAwait(false);
        if (dto is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        return Ok(
            new RegistrationLegalRequirementsResponse(
                dto.TermsOfUseVersionId,
                dto.PrivacyPolicyVersionId,
                dto.TermsTitle,
                dto.PrivacyTitle));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterPublicRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator
            .Send(
                new RegisterTorcedorCommand(
                    request.Name,
                    request.Email,
                    request.Password,
                    request.PhoneNumber,
                    request.AcceptedLegalDocumentVersionIds),
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors });

        var session = await auth.IssueSessionForUserIdAsync(result.UserId!.Value, cancellationToken).ConfigureAwait(false);
        if (session is null)
            return StatusCode(StatusCodes.Status500InternalServerError);
        return Ok(session);
    }

    [HttpGet("digital-card")]
    [Authorize]
    public async Task<ActionResult<MyDigitalCardViewDto>> GetMyDigitalCard(CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();
        var dto = await mediator.Send(new GetMyDigitalCardQuery(userId.Value), cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<MyProfileResponse>> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();
        var profile = await mediator.Send(new GetMyProfileQuery(userId.Value), cancellationToken).ConfigureAwait(false);
        if (profile is null)
            return NotFound();
        return Ok(
            new MyProfileResponse(profile.Document, profile.BirthDate, profile.PhotoUrl, profile.Address));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpsertProfile([FromBody] UpsertMyProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();
        var ok = await mediator
            .Send(
                new UpsertMyProfileCommand(
                    userId.Value,
                    new MyProfileUpsertDto(request.Document, request.BirthDate, request.PhotoUrl, request.Address)),
                cancellationToken)
            .ConfigureAwait(false);
        if (!ok)
            return NotFound();
        return NoContent();
    }

    [HttpPost("profile/photo")]
    [Authorize]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ProfilePhotoUploadResponse>> UploadPhoto(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest();
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        await using var stream = file.OpenReadStream();
        var url = await photoStorage
            .SaveProfilePhotoAsync(userId.Value, stream, file.FileName, file.ContentType, cancellationToken)
            .ConfigureAwait(false);
        if (url is null)
            return BadRequest();

        var ok = await mediator
            .Send(
                new UpsertMyProfileCommand(userId.Value, new MyProfileUpsertDto(null, null, url, null)),
                cancellationToken)
            .ConfigureAwait(false);
        if (!ok)
            return NotFound();

        return Ok(new ProfilePhotoUploadResponse(url));
    }

    private Guid? GetUserIdOrDefault()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var g) ? g : null;
    }
}
