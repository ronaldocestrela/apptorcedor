using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminSupportAttachmentDownload;

public sealed class GetAdminSupportAttachmentDownloadQueryHandler(ISupportAdministrationPort administration)
    : IRequestHandler<GetAdminSupportAttachmentDownloadQuery, SupportAttachmentDownloadDto?>
{
    public Task<SupportAttachmentDownloadDto?> Handle(
        GetAdminSupportAttachmentDownloadQuery request,
        CancellationToken cancellationToken) =>
        administration.GetSupportAttachmentDownloadAsync(request.TicketId, request.AttachmentId, cancellationToken);
}
