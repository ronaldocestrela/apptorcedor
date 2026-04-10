using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Identity.Application.DTOs;

namespace SocioTorcedor.Modules.Identity.Application.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    Guid AcceptedTermsDocumentId,
    Guid AcceptedPrivacyDocumentId,
    string? ConsentIpAddress,
    string? ConsentUserAgent) : ICommand<AuthResultDto>;
