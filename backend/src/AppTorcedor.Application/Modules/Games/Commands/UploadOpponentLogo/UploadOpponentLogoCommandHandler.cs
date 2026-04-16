using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Games.Commands.UploadOpponentLogo;

public sealed class UploadOpponentLogoCommandHandler(IOpponentLogoLibraryAdminPort library)
    : IRequestHandler<UploadOpponentLogoCommand, UploadOpponentLogoResult?>
{
    public Task<UploadOpponentLogoResult?> Handle(UploadOpponentLogoCommand request, CancellationToken cancellationToken) =>
        library.UploadAndRegisterAsync(request.Content, request.FileName, request.ContentType, cancellationToken);
}
