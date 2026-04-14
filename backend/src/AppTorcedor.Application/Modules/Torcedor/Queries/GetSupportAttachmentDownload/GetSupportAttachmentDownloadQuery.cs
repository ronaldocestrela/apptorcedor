using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetSupportAttachmentDownload;

public sealed record GetSupportAttachmentDownloadQuery(Guid UserId, Guid TicketId, Guid AttachmentId)
    : IRequest<SupportAttachmentDownloadDto?>;
