using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Games.Commands.UploadOpponentLogo;

public sealed record UploadOpponentLogoCommand(Stream Content, string FileName, string ContentType)
    : IRequest<UploadOpponentLogoResult?>;
