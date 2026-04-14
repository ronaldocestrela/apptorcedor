using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminSupportAttachmentDownload;

public sealed record GetAdminSupportAttachmentDownloadQuery(Guid TicketId, Guid AttachmentId)
    : IRequest<SupportAttachmentDownloadDto?>;
