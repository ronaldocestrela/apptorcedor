using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateAdminNews;

public sealed class CreateAdminNewsCommandHandler(INewsAdministrationPort news)
    : IRequestHandler<CreateAdminNewsCommand, NewsCreateResult>
{
    public Task<NewsCreateResult> Handle(CreateAdminNewsCommand request, CancellationToken cancellationToken) =>
        news.CreateNewsAsync(request.Dto, cancellationToken);
}
