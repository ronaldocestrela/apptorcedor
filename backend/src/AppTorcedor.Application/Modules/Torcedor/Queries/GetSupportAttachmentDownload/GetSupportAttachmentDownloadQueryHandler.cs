using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetSupportAttachmentDownload;

public sealed class GetSupportAttachmentDownloadQueryHandler(ISupportAdministrationPort administration)
    : IRequestHandler<GetSupportAttachmentDownloadQuery, SupportAttachmentDownloadDto?>
{
    public async Task<SupportAttachmentDownloadDto?> Handle(
        GetSupportAttachmentDownloadQuery request,
        CancellationToken cancellationToken)
    {
        var row = await administration
            .GetSupportAttachmentDownloadAsync(request.TicketId, request.AttachmentId, cancellationToken)
            .ConfigureAwait(false);
        if (row is null || row.RequesterUserId != request.UserId)
            return null;
        return row;
    }
}
