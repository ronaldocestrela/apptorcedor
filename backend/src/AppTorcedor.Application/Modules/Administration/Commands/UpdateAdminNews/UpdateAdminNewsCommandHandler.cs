using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateAdminNews;

public sealed class UpdateAdminNewsCommandHandler(INewsAdministrationPort news)
    : IRequestHandler<UpdateAdminNewsCommand, NewsMutationResult>
{
    public Task<NewsMutationResult> Handle(UpdateAdminNewsCommand request, CancellationToken cancellationToken) =>
        news.UpdateNewsAsync(request.NewsId, request.Dto, cancellationToken);
}
