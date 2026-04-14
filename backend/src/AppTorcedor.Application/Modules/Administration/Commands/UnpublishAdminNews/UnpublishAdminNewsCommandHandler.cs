using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UnpublishAdminNews;

public sealed class UnpublishAdminNewsCommandHandler(INewsAdministrationPort news)
    : IRequestHandler<UnpublishAdminNewsCommand, NewsMutationResult>
{
    public Task<NewsMutationResult> Handle(UnpublishAdminNewsCommand request, CancellationToken cancellationToken) =>
        news.UnpublishNewsAsync(request.NewsId, cancellationToken);
}
