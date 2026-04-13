using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.PublishAdminNews;

public sealed class PublishAdminNewsCommandHandler(INewsAdministrationPort news)
    : IRequestHandler<PublishAdminNewsCommand, NewsMutationResult>
{
    public Task<NewsMutationResult> Handle(PublishAdminNewsCommand request, CancellationToken cancellationToken) =>
        news.PublishNewsAsync(request.NewsId, cancellationToken);
}
